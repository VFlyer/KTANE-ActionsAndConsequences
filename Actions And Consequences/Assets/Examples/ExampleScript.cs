using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ExampleScript : MonoBehaviour {

   public KMBombInfo Bomb;
    public KMBombModule Module;
   public KMAudio Audio;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

    public KMSelectable OnlyButton;
    public TextMesh OnlyText;
   void Awake () {
      ModuleId = ModuleIdCounter++;
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */
        OnlyButton.OnInteract += delegate () {
            ModuleSolved = true;
            Module.HandlePass();
            OnlyText.text = "YOU DID IT!";
            return false; };
      //button.OnInteract += delegate () { buttonPress(); return false; };

   }

   void Start () {

   }

   void Update () {

   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
   }

   IEnumerator TwitchHandleForcedSolve () {
      yield return null;
   }
}
