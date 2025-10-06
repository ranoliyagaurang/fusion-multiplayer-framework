using System;
using UnityEngine;

namespace PTTI_Multiplayer
{
    [CreateAssetMenu(fileName = "PPTListSO", menuName = "HVAC/PPT List")]
    public class PPTList : ScriptableObject
    {
        public PPTEntry[] pPTEntries;
    }

    [Serializable]
    public class PPTEntry
    {
        public string pptName;
        public Texture2D thumbnail;
        public Slide_List slide_List;
    }
}