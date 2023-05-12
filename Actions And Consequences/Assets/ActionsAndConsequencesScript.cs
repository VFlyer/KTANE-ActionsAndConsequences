using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ActionsAndConsequencesScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBossModule BossModule;

    /* buttons */
    public KMSelectable[] Buttons;
    public TextMesh[] ButtonLabels;

    /* top display */
    public TextMesh TopDisplay;

    /* side display */
    public SpriteRenderer[] Blocks;
    public TextMesh[] BlockDisplays;
    public Color[] BlockColors; //0 green, 1 red, 2 yellow
    public Color[] BlockDisplayColors; //0 green, 1 red, 2 yellow

    float[] BlockDisplayZPositions = new float[5];

    /* internal */
    List<char> RandomizedLetters = new List<char>();
    List<int> BlockList = new List<int>();
    //00 = green, 01 = red, 11-99 = yellow
    //is there a more efficient way to do this? probably. will i use it? :joy:
    int solvedModuleCount = 0, ignoredSolves = 0, strikes = 0, blockCount;
    int solvableModules;

    bool inputMode;
    int inputLength;

    private string MostRecentSolve;
    private string[] IgnoredModules;
    private List<string> SolvedModules = new List<string>();

    /* timer */
    public float delay;
    float timer;
    const int SHORTEST = 30;
    const int LONGEST = 75;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake () {
        ModuleId = ModuleIdCounter++;
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */
        foreach(KMSelectable button in Buttons)
        {
            button.OnInteract += delegate ()
            {

                return false;
            };
        }
        //button.OnInteract += delegate () { buttonPress(); return false; };
    }

    void Start () {
        delay = (float)(Rnd.Range(SHORTEST, LONGEST+1));
        Debug.LogFormat("[Actions and Consequences #{0}] Next number block will fall in {1} seconds.", ModuleId, delay);
        TopDisplay.text = "Action!";

        solvableModules = Bomb.GetSolvableModuleNames().Count;
        Debug.LogFormat("[Actions and Consequences #{0}] solvableModules = {1}", ModuleId, Bomb.GetSolvableModuleNames().Count);

        if (IgnoredModules == null)
            IgnoredModules = BossModule.GetIgnoredModules("Actions and Consequences", new string[] {
            //"Template 1",
            "Actions and Consequences"
            });

        //getting original z positions of blocks for later because pebble brain cerulean cannot think of a more efficient way to do this
        for (int i = 0; i < 5; i++)
        {
            BlockDisplayZPositions[i] = BlockDisplays[i].transform.position.z;
        }

        //deactivate blocks
        foreach (SpriteRenderer block in Blocks)
        {
            block.gameObject.SetActive(false);
        }

        //keypad letters
        RandomizedLetters = GenerateRandomLetters();
        String RandomizedStr = "";
        for(int i = 0; i < RandomizedLetters.Count; i++)
        {
            RandomizedStr += RandomizedLetters.ElementAt(i);
        }
        Debug.LogFormat("[Actions and Consequences #{0}] Letters on keypad in reading order: {1}", ModuleId, RandomizedStr);
        for (int i = 0; i < 9; i++)
        {
            ButtonLabels[i].text = RandomizedLetters.ElementAt(i) + "";
        }
    }
    List<char> GenerateRandomLetters ()
    {
        List<char> alphabetList = new List<char>();
        String alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        foreach(char c in alphabet)
        {
            alphabetList.Add(c);
        }

        List<char> output = new List<char>();
        for(int i = 0; i < 9; i++)
        {
            int rand = Rnd.Range(0, alphabetList.Count);
            output.Add(alphabetList.ElementAt(rand));
            alphabetList.RemoveAt(rand);
        }
        return output;
    }
    void UpdateBlockDisplay()
    {
        if(blockCount >= 5)
        {
            for(int i = 0; i < 5; i++)
            {
                UpdateBlock(i, BlockList.ElementAt(blockCount - 5 + i));
            }
        }
        else
        {
            UpdateBlock(blockCount - 1, BlockList.ElementAt(blockCount - 1));
        }
    }
    void UpdateBlock(int visualIndex, int blockID)
    {
        Blocks[visualIndex].gameObject.SetActive(true);
        Blocks[visualIndex].color = BlockColors[
            blockID == 0 ? 0 :
            blockID == 1 ? 1 :
            2];
        BlockDisplays[visualIndex].color = BlockDisplayColors[
            blockID == 0 ? 0 :
            blockID == 1 ? 1 :
            2];
        switch (blockID)
        {
            case 0:
                BlockDisplays[visualIndex].text = "✓";
                BlockDisplays[visualIndex].transform.position = new Vector3(Blocks[visualIndex].transform.position.x, Blocks[visualIndex].transform.position.y, BlockDisplayZPositions[visualIndex]);
                break;
            case 1:
                BlockDisplays[visualIndex].text = "✗";
                BlockDisplays[visualIndex].transform.position = new Vector3(Blocks[visualIndex].transform.position.x, Blocks[visualIndex].transform.position.y, BlockDisplayZPositions[visualIndex] + 0.0008f);
                break;
            default:
                BlockDisplays[visualIndex].text = blockID + "";
                BlockDisplays[visualIndex].transform.position = new Vector3(Blocks[visualIndex].transform.position.x, Blocks[visualIndex].transform.position.y, BlockDisplayZPositions[visualIndex] + 0.0005f);
                break;
        }
    }
    int GenerateYellowValue()
    {
        int output = 0;
        while(output % 10 == 0)
        {
            output = Rnd.Range(11, 100);
        }
        return output;
    }
    void GenerateNewBlock(int blockID)
    {
        switch(blockID)
        {
            case 0:
                Audio.PlaySoundAtTransform("solve", transform);
                Debug.LogFormat("[Actions and Consequences #{0}] Spawned solve block.", ModuleId);
                break;
            case 1:
                Audio.PlaySoundAtTransform("consequence", transform);
                Debug.LogFormat("[Actions and Consequences #{0}] Spawned strike block.", ModuleId);
                break;
            default:
                Audio.PlaySoundAtTransform("action", transform);
                Debug.LogFormat("[Actions and Consequences #{0}] Spawned number block with value {1}.", ModuleId, blockID);
                break;
        }
        BlockList.Add(blockID);
        blockCount++;
        UpdateBlockDisplay();
    }
    void Update () {
        if (ModuleSolved || inputMode)
            return;
        timer += Time.deltaTime;
        if(timer > delay)
        {
            GenerateNewBlock(GenerateYellowValue());
            delay = (float)(Rnd.Range(SHORTEST, LONGEST + 1));
            Debug.LogFormat("[Actions and Consequences #{0}] Next number block will fall in {1} seconds.", ModuleId, delay);
            timer = 0;
        }

        //THANK YOU BLANANAS2
        solvedModuleCount = Bomb.GetSolvedModuleNames().Count;
        if (solvedModuleCount > SolvedModules.Count())
        {
            //solvableModules = Bomb.GetSolvableModuleNames().Count;
            //Debug.LogFormat("[Actions and Consequences #{0}] solvableModules = {1}", ModuleId, Bomb.GetSolvableModuleNames().Count);

            MostRecentSolve = GetLatestSolve(Bomb.GetSolvedModuleNames(), SolvedModules);
            if (!(IgnoredModules.Contains(MostRecentSolve)))
            {
                GenerateNewBlock(0);
                SolvedModules.Add(MostRecentSolve);
            }
            else
            {
                Debug.LogFormat("[Actions and Consequences #{0}] Ignored module has been solved: {1}", ModuleId, MostRecentSolve);
                ignoredSolves++;
                SolvedModules.Add(MostRecentSolve);
            }
        }

        if(Bomb.GetStrikes() > strikes)
            GenerateNewBlock(1);
        //if(Bomb.GetSolvedModuleNames().Count > solvedModuleCount)
        //    GenerateNewBlock(0);
        strikes = Bomb.GetStrikes();

        if(SolvedModules.Count - ignoredSolves == solvableModules - IgnoredModules.Count())
        {
            Debug.LogFormat("[Actions and Consequences #{0}] Entering input mode in 10 seconds from {1}.", ModuleId, Bomb.GetFormattedTime());
            inputMode = true;
            StartCoroutine(EnterInputMode());
        }
    }
    string GetLatestSolve(List<string> a, List<string> b) // THANK YOU BLANANAS2 AND EXISH
    {
        List<string> tempA = a;
        List<string> tempB = b;
        for(int i = 0; i < b.Count; i++)
        {
            tempA.Remove(tempB.ElementAt(i));
        }
        return tempA.ElementAt(0);
    }
    IEnumerator EnterInputMode()
    {
        TopDisplay.text = "";
        yield return new WaitForSeconds(10f);
        foreach (SpriteRenderer block in Blocks)
        {
            block.gameObject.SetActive(false);
        }
        Debug.LogFormat("[Actions and Consequences #{0}] Entering input mode at {1}.", ModuleId, Bomb.GetFormattedTime());
        yield return null;
    }

    //------------- Kuro my beloved <3 -------------
    //#pragma warning disable 414
    //   private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
    //#pragma warning restore 414

    //   IEnumerator ProcessTwitchCommand (string Command) {
    //      yield return null;
    //   }

    //   IEnumerator TwitchHandleForcedSolve () {
    //      yield return null;
    //   }
}
