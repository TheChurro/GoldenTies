using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour {
    [System.Serializable]
    public struct TaggedDialog {
        public string Speaker;
        public string Content;
    }
    [System.Serializable]
    public struct Interaction {
        public string flagNeeded;
        public TaggedDialog[] dialog;
        public string reward;
        public string[] flagsSet;
        public string[] flagsRemove;
    }
    public Interaction[] interactions;
    public int GetInteraction(List<string> flags) {
        for (int i = 0; i < interactions.Length; i++) {
            var flagNeeded = interactions[i].flagNeeded;
            if (flagNeeded.Equals("") || flags.Contains(flagNeeded)) {
                return i;
            }
        }
        return -1;
    }
}