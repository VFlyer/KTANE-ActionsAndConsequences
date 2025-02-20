﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
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

    /* internal */
    List<char> RandomizedLetters = new List<char>();
    List<int> BlockList = new List<int>();
    /* 00 = green, 01 = red, 11-99 = yellow */
    /* is there a more efficient way to do this? probably. will i use it? :joy: */
    int Solves;
    int IgnoredSolved;
    int strikes = 0;
    int NonBosses = 1;
    int NumberOfAC = 0;

    int blockCount;
    //int solvableModules;

    bool inputMode;
    int inputLength;
    string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private string MostRecent;
    private string[] IgnoredModules;
    private List<string> SolveList = new List<string>();
    private List<string> IgnoredSolveds = new List<string>();

    string solution = "";
    int[,] numberMap = new int[9,9]
    {
        { 4, 2, 7, 6, 8, 5, 9, 1, 3 },
        { 9, 1, 5, 4, 2, 3, 6, 8, 7 },
        { 6, 8, 3, 9, 7, 1, 2, 5, 4 },
        { 3, 4, 9, 5, 1, 8, 7, 2, 6 },
        { 8, 7, 1, 2, 6, 9, 3, 4, 5 },
        { 2, 5, 6, 7, 3, 4, 8, 9, 1 },
        { 1, 3, 2, 8, 5, 6, 4, 7, 9 },
        { 7, 6, 4, 1, 9, 2, 5, 3, 8 },
        { 5, 9, 8, 3, 4, 7, 1, 6, 2 }
    };
    int solutionPointer = 0;
    bool recovery;
    int lastBlockToDisplayDuringRecovery = 0; //indexed at 1

    /* timer */
    public float delay;
    float timer;
    const int SHORTEST = 30; //30
    const int LONGEST = 75; //75

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved, Activated;

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
                if (inputMode)
                {
                    CheckPress(button);
                    button.AddInteractionPunch();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
                }
                return false;
            };
        }
        //solvableModules = Bomb.GetSolvableModuleNames().Count;
        //Debug.LogFormat("[Actions and Consequences #{0}] (Awake) solvableModules = {1}", ModuleId, Bomb.GetSolvableModuleNames().Count);

        //button.OnInteract += delegate () { buttonPress(); return false; };
    }

    void Start () {
        //THANK YOU SO MUCH BLAN
        if (IgnoredModules == null)
            IgnoredModules = BossModule.GetIgnoredModules("Actions and Consequences", new string[] {
            "Forget Me Not",
            "Souvenir",
            "Forget Everything",
            "Simon's Stages",
            "Forget This",
            "Purgatory",
            "The Troll",
            "Forget Them All",
            "Tallordered Keys",
            "Forget Enigma",
            "Forget Us Not",
            "Forget Perspective",
            "Organization",
            "The Very Annoying Button",
            "Forget Me Later",
            "Übermodule",
            "Ultimate Custom Night",
            "14",
            "Forget It Not",
            "Simon Forgets",
            "Brainf---",
            "Forget The Colors",
            "RPS Judging",
            "The Twin",
            "Iconic",
            "OmegaForget",
            "Kugelblitz",
            "A>N<D",
            "Don't Touch Anything",
            "Busy Beaver",
            "Whiteout",
            "Forget Any Color",
            "Keypad Directionality",
            "Security Council",
            "Shoddy Chess",
            "Floor Lights",
            "Black Arrows",
            "Forget Maze Not",
            "+",
            "Soulscream",
            "Cube Synchronization",
            "Out of Time",
            "Tetrahedron",
            "The Board Walk",
            "Gemory",
            "Duck Konundrum",
            "Concentration",
            "Twister",
            "Forget Our Voices",
            "Soulsong",
            "ID Exchange",
            "8",
            "Remember Simple",
            "Remembern't Simple",
            "The Grand Prix",
            "Forget Me Maybe",
            "HyperForget",
            "Bitwise Oblivion",
            "Damocles Lumber",
            "Top 10 Numbers",
            "Queen’s War",
            "Forget Fractal",
            "Pointer Pointer",
            "Slight Gibberish Twist",
            "Piano Paradox",
            "OMISSION",
            "In Order",
            "The Nobody’s Code",
            "Perspective Stacking",
            "Reporting Anomalies",
            "Forgetle",
            "Actions and Consequences",
            "X",
            "Y",
            "Castor",
            "Pollux",
            "Apple Pen",
            "Pineapple Pen",
            "Reporting Anomalies",
            "Turn The Key",
            "The Time Keeper",
            "Timing is Everything",
            "Bamboozling Time Keeper",
            "Password Destroyer",
            "OmegaDestroyer",
            "Zener Cards",
            "Doomsday Button",
            "Red Light Green Light"
            });
        Debug.LogFormat("[Actions and Consequences #{0}] Ignored module count: {1}", ModuleId, IgnoredModules.Length);
        Module.OnActivate += delegate ()
        {
            NonBosses = Bomb.GetSolvableModuleNames().Where(a => !IgnoredModules.Contains(a)).ToList().Count; //WHAT THE FUCK IS A KILOMETER
            Activated = true;
        };
        for(int i = 0; i < Bomb.GetModuleNames().Count; i++)
        {
            if (Bomb.GetModuleNames()[i] == "Actions and Consequences")
            {
                NumberOfAC++;
            }
            else continue;
        }

        delay = (float)(Rnd.Range(SHORTEST, LONGEST+1));
        Debug.LogFormat("[Actions and Consequences #{0}] Next number block will fall in {1} seconds.", ModuleId, delay);
        TopDisplay.text = "";

        //solvableModules = Bomb.GetSolvableModuleNames().Count;

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
        _labels = new char[ButtonLabels.Length];
        for (int i = 0; i < 9; i++)
        {
            char newLetter = RandomizedLetters.ElementAt(i);
            ButtonLabels[i].text = newLetter.ToString();
            _labels[i] = newLetter;
        }

        AddToSolution(-1);
    }
    List<char> GenerateRandomLetters ()
    {
        List<char> alphabetList = new List<char>();
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
    void AddToSolution(int blockID) //if blockID = -1 it will calculate the first letter
    {
        int blockCount = BlockList.Count;
        char prevChar;
        int posOfPrevChar;
        switch (blockID)
        {
            case -1: // first letter
                string firstLetterSerialStr = Bomb.GetSerialNumber().ElementAt(0) + "";
                if(alphabet.Contains(firstLetterSerialStr))
                {
                    string doubleAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    char firstLetterSerialChar = firstLetterSerialStr.ElementAt(0);
                    while(!RandomizedLetters.Contains(firstLetterSerialChar))
                    {
                        firstLetterSerialChar = doubleAlphabet.ElementAt(doubleAlphabet.IndexOf(firstLetterSerialChar)+1);
                    }
                    solution += firstLetterSerialChar;
                }
                else
                {
                    int num = Int16.Parse(firstLetterSerialStr) % 9 - 1;
                    if (num < 0)
                        num = 8;
                    solution += RandomizedLetters.ElementAt(num);
                }
                break;
            case 0: // solve block
                prevChar = solution.ElementAt(solution.Length - 1);
                int solveBlockCount = 0;
                foreach(int block in BlockList)
                {
                    solveBlockCount += block == 0 ? 1 : 0;
                }
                posOfPrevChar = RandomizedLetters.IndexOf(prevChar);
                solution += RandomizedLetters.ElementAt((posOfPrevChar + solveBlockCount) % 9);
                break;
            case 1: // strike block
                prevChar = solution.ElementAt(solution.Length - 1);                
                solution += prevChar;
                for(int i = 1; i < 9; i++)
                {
                    solution += RandomizedLetters.ElementAt((RandomizedLetters.IndexOf(prevChar)+i)%9);
                }
                solveBlockCount = 0;
                foreach (int block in BlockList)
                {
                    solveBlockCount += block == 0 ? 1 : 0;
                }
                solution += RandomizedLetters.ElementAt(solveBlockCount % 9);
                break;
            default: // number block
                int r = blockID / 10 - 1;
                int c = blockID % 10 - 1;
                int intersect = numberMap[r,c];
                Debug.LogFormat("[Actions and Consequences #{0}] For block #{1} with value {2}, first number is {3}.", ModuleId, blockCount, blockID, intersect);
                prevChar = solution.ElementAt(solution.Length - 1);
                posOfPrevChar = RandomizedLetters.IndexOf(prevChar);
                int posOfSolution;
                switch (posOfPrevChar)
                {
                    case 0:
                        for(int i = 0; i < intersect; i++)
                        {
                            r--;
                            c--;
                            if (r < 0)
                                r = 8;
                            if (c < 0)
                                c = 8;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                    case 1:
                        for (int i = 0; i < intersect; i++)
                        {
                            r--;
                            if (r < 0)
                                r = 8;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                    case 2:
                        for (int i = 0; i < intersect; i++)
                        {
                            r--;
                            c++;
                            if (r < 0)
                                r = 8;
                            if (c > 8)
                                c = 0;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                    case 3:
                        for (int i = 0; i < intersect; i++)
                        {
                            c--;
                            if (c < 0)
                                c = 8;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                    case 4:
                        posOfSolution = intersect;
                        break;
                    case 5:
                        for (int i = 0; i < intersect; i++)
                        {
                            c++;
                            if (c > 8)
                                c = 0;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                    case 6:
                        for (int i = 0; i < intersect; i++)
                        {
                            r++;
                            c--;
                            if (r > 8)
                                r = 0;
                            if (c < 0)
                                c = 8;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                    case 7:
                        for (int i = 0; i < intersect; i++)
                        {
                            r++;
                            if (r > 8)
                                r = 0;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                    default: //case 8
                        for (int i = 0; i < intersect; i++)
                        {
                            r++;
                            c++;
                            if (r > 8)
                                r = 0;
                            if (c > 8)
                                c = 0;
                        }
                        posOfSolution = numberMap[r, c];
                        break;
                }
                Debug.LogFormat("[Actions and Consequences #{0}] For block #{1} with value {2}, the position of the solution is {3}.", ModuleId, blockCount, blockID, posOfSolution);
                solution += RandomizedLetters.ElementAt(posOfSolution-1);
                break;
        }
        Debug.LogFormat("[Actions and Consequences #{0}] The solution is now \"{1}\".", ModuleId, solution);
    }
    void CheckPress(KMSelectable button)
    {
        int buttonPos = Array.IndexOf(Buttons, button);
        if (!recovery)
        {
            char correctInput = solution.ElementAt(solutionPointer);
            Debug.LogFormat("[Actions and Consequences #{0}] Button pressed at index {1}, button reads {2}", ModuleId, buttonPos, ButtonLabels[buttonPos].text);
            if (ButtonLabels[buttonPos].text.Equals(correctInput + ""))
            {
                // UNcommented display method of hiding first letter when it gets too long
                TopDisplay.text = solutionPointer == 0 ? correctInput + "" :
                    solutionPointer < 7 ? TopDisplay.text + correctInput :
                    TopDisplay.text.Substring(1) + correctInput;

                // commented out display method of clearing display when it gets too long
                //TopDisplay.text = TopDisplay.text.Length > 6 ? correctInput + "" : TopDisplay.text + correctInput;

                solutionPointer++;
                if (solutionPointer >= solution.Length)
                {
                    StartCoroutine(YouDidIt());
                }
            }
            else
            {
                Debug.LogFormat("[Actions and Consequences #{0}] Struck; button pressed read {1}, expected {2}", ModuleId, ButtonLabels[buttonPos].text, correctInput);
                Module.HandleStrike();
                EnterRecovery();
            }
        }
        else
        {
            switch(buttonPos)
            {
                case 1:
                    if (lastBlockToDisplayDuringRecovery < BlockList.Count)
                    {
                        UpdateBlockDisplay(lastBlockToDisplayDuringRecovery + 1);
                        lastBlockToDisplayDuringRecovery++;
                    }
                    break;
                case 7:
                    if(lastBlockToDisplayDuringRecovery > 5)
                    {
                        UpdateBlockDisplay(lastBlockToDisplayDuringRecovery - 1);
                        lastBlockToDisplayDuringRecovery--;
                    }
                    break;
                case 4:
                    UpdateBlockDisplay(-1); //clears blocks
                    lastBlockToDisplayDuringRecovery = lastBlockToDisplayDuringRecovery < 5 ? BlockList.Count : 5;
                    TopDisplay.text = solutionPointer > 6 ? solution.Substring(solutionPointer - 5, 7) : solution.Substring(0, solutionPointer);
                    recovery = false;
                    break;
                default:
                    break;
            }
        }
    }
    void EnterRecovery()
    {
        recovery = true;
        UpdateBlockDisplay(lastBlockToDisplayDuringRecovery);
        TopDisplay.text = "        !";
    }
    IEnumerator YouDidIt()
    {
        ModuleSolved = true;
        Module.HandlePass();
        TopDisplay.text = "Well done!";
        yield return new WaitForSeconds(3.0f);
        //THANK YOU COOLDOOM5 (The Great Void)
        byte a = 255;
        while(a > 0)
        {
            a -= 5;
            TopDisplay.color = new Color32(60, 255, 0, a);
            yield return new WaitForSeconds(0.02f);
        }
    }
    void UpdateBlockDisplay(int lastBlockToDisplay) //lastBlockToDisplay indexed at 1
    {
        if(lastBlockToDisplay >= 5)
        {
            for(int i = 0; i < 5; i++)
            {
                UpdateBlock(i, BlockList.ElementAt(lastBlockToDisplay - 5 + i));
            }
        }
        else if(lastBlockToDisplay == -1) //clears blocks
        {
            for (int i = 0; i < 5; i++)
            {
                Blocks[i].gameObject.SetActive(false);
            }
        }
        else
        {
            for (int i = 0; i < lastBlockToDisplay; i++)
            {
                UpdateBlock(i, BlockList.ElementAt(i));
            }
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
                //BlockDisplays[visualIndex].transform.position = new Vector3(Blocks[visualIndex].transform.position.x, Blocks[visualIndex].transform.position.y + 0.004f, Blocks[visualIndex].transform.position.z);
                break;
            case 1:
                BlockDisplays[visualIndex].text = "✗";
                //BlockDisplays[visualIndex].transform.position = new Vector3(Blocks[visualIndex].transform.position.x, Blocks[visualIndex].transform.position.y + 0.004f, Blocks[visualIndex].transform.position.z + 0.0008f);
                break;
            default:
                BlockDisplays[visualIndex].text = blockID + "";
                //BlockDisplays[visualIndex].transform.position = new Vector3(Blocks[visualIndex].transform.position.x, Blocks[visualIndex].transform.position.y + 0.004f, Blocks[visualIndex].transform.position.z + 0.0005f);
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
        if (lastBlockToDisplayDuringRecovery < 5)
            lastBlockToDisplayDuringRecovery++;
        AddToSolution(blockID);
        UpdateBlockDisplay(blockCount);
    }
    void Update () {
        if (ModuleSolved || inputMode || TopDisplay.text.Equals("Action!"))
            return;
        if (Activated)
            timer += Time.deltaTime;
        if(timer > delay)
        {
            GenerateNewBlock(GenerateYellowValue());
            delay = (float)(Rnd.Range(SHORTEST, LONGEST + 1));
            Debug.LogFormat("[Actions and Consequences #{0}] At {2}, next number block will fall in {1} seconds.", ModuleId, delay, Bomb.GetFormattedTime());
            timer = 0;
        }
        //always sets the things to the thing
        //for(int i = 0; i < 5; i++)
        //{
        //    BlockDisplays[i].transform.position = BlockDisplays[i].text.Equals("✓") ? new Vector3(Blocks[i].transform.position.x, Blocks[i].transform.position.y + 0.004f, Blocks[i].transform.position.z) :
        //        BlockDisplays[i].text.Equals("✗") ? new Vector3(Blocks[i].transform.position.x, Blocks[i].transform.position.y + 0.004f, Blocks[i].transform.position.z + 0.0008f) :
        //        new Vector3(Blocks[i].transform.position.x, Blocks[i].transform.position.y + 0.004f, Blocks[i].transform.position.z + 0.0005f);
        //}
        //THANK YOU BLANANAS2
        Solves = Bomb.GetSolvedModuleNames().Count;
        if (Solves > SolveList.Count)
        {
            //solvableModules = Bomb.GetSolvableModuleNames().Count;
            //Debug.LogFormat("[Actions and Consequences #{0}] solvableModules = {1}", ModuleId, Bomb.GetSolvableModuleNames().Count);

            MostRecent = GetLatestSolve(Bomb.GetSolvedModuleNames(), SolveList);
            //Debug.LogFormat("[Actions and Consequences #{0}] Most recent solve: {1}, SolveList.Count: {2}", ModuleId, MostRecent, SolveList.Count);
            if (!(IgnoredModules.Contains(MostRecent)))
            {
                GenerateNewBlock(0);
                SolveList.Add(MostRecent);
            }
            else
            {
                Debug.LogFormat("[Actions and Consequences #{0}] Ignored module has been solved: {1}", ModuleId, MostRecent);
                SolveList.Add(MostRecent);
                IgnoredSolved++;
            }
        }

        if (Bomb.GetStrikes() > strikes)
            GenerateNewBlock(1);
        strikes = Bomb.GetStrikes();
        
        //Debug.LogFormat("[Actions and Consequences #{0}] Solved modules ({1}) - Solved ignored modules ({2}) = {3} ; expect {4}", ModuleId, Bomb.GetSolvedModuleNames().Count, IgnoredSolveds.Count, Bomb.GetSolvedModuleNames().Count - IgnoredSolveds.Count, NonBosses);
        if(SolveList.Count - IgnoredSolved == NonBosses)
        {
            if (/*Bomb.GetSolvableModuleNames().Count - IgnoredModules.Length <= 0*/ NonBosses == 0 || solution.Length == 1)
                StartCoroutine(YouDidIt());
            else
            {
                Debug.LogFormat("[Actions and Consequences #{0}] Entering input mode in 10 seconds from {1}.", ModuleId, Bomb.GetFormattedTime());
                StartCoroutine(EnterInputMode());
            }
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
        TopDisplay.text = "Action!";
        yield return new WaitForSeconds(10f);
        foreach (SpriteRenderer block in Blocks)
        {
            block.gameObject.SetActive(false);
        }
        TopDisplay.text = "-  -  -  -  -";
        Debug.LogFormat("[Actions and Consequences #{0}] Entering input mode at {1}.", ModuleId, Bomb.GetFormattedTime());
        inputMode = true;
        yield return null;
    }

    //------------- Kuro my beloved <3 -------------
    //-------------------- <3 - Kuro ---------------

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use '!{0} press <buttons>' to press those buttons; buttons are 1-9 in reading order, or you can use their labels instead.";
#pragma warning restore 414

    private bool TwitchShouldCancelCommand;
    private char[] _spaceLol = " ".ToCharArray();
    private char[] _labels;

    private IEnumerator ProcessTwitchCommand(string command) {
        string[] commandSplit = command.Trim().ToUpper().Split(_spaceLol, 2);

        if (commandSplit.Length != 2) {
            yield return "sendtochaterror Invalid command!";
        }

        // Mandate the use of "PRESS" to prevent accidental inputs.
        if (commandSplit[0] != "PRESS") {
            yield return "sendtochaterror You must start the command with 'press', for example '!{1} press 123'";
        }

        string presses = commandSplit[1].Replace(" ", string.Empty);
        var positionsToPress = new List<int>();
        foreach (char p in presses) {
            if (char.IsDigit(p) && int.Parse(p.ToString()) != 0) {
                positionsToPress.Add(int.Parse(p.ToString()) - 1);
            }
            else if (_labels.Contains(p)) {
                positionsToPress.Add(Array.IndexOf(_labels, p));
            }
            else {
                yield return "sendtochaterror '" + p + "' is not a valid button to press!";
            }
        }

        yield return null;
        for (int p = 0, q = positionsToPress.Count(); p < q; p++) {
            Buttons[positionsToPress[p]].OnInteract();
            yield return new WaitForSeconds(0.1f);
            if (TwitchShouldCancelCommand) {
                yield return "sendtochat {0}, Actions and Consequences (id: {1}) processed the first " + (p + 1) + " presses before cancelling.";
                yield return "cancelled";
            }
        }
    }

    private IEnumerator TwitchHandleForcedSolve() {
        while (!inputMode) {
            yield return true;
        }

        string remainingSolution = solution.Substring(solutionPointer);

        if (recovery) {
            yield return ProcessTwitchCommand("press 5" + remainingSolution);
        }
        else {

            yield return ProcessTwitchCommand("press " + remainingSolution);
        }
    }
}
