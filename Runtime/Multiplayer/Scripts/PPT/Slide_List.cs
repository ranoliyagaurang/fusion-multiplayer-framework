using System;
using System.Collections.Generic;
using UnityEngine;

namespace PTTI_Multiplayer
{
    [CreateAssetMenu(fileName = "Slides", menuName = "HVAC/Slide List")]
    public class Slide_List : ScriptableObject
    {
        public Vector2 imageSize;
        public List<PresentationSlide> slides = new();
    }

    [Serializable]
    public class PresentationSlide
    {
        public Sprite sprite;
    }
}