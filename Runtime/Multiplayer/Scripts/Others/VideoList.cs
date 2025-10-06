using System;
using UnityEngine;
using UnityEngine.Video;

namespace PTTI_Multiplayer
{
    [CreateAssetMenu(fileName = "VideoListSO", menuName = "HVAC/Video Clip List")]
    public class VideoList : ScriptableObject
    {
        public VideoEntry[] videos;
    }

    [Serializable]
    public class VideoEntry
    {
        public string videoName;
        public VideoClip videoClip;
        public Texture2D thumbnail;
        public bool useURL = false;
        public string videoURL;

        // Predefined jump times (in seconds)
        public double[] jumpTimes;
    }
}