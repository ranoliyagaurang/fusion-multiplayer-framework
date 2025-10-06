using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PTTI_Multiplayer
{
    [CreateAssetMenu(fileName = "SceneListSO", menuName = "HVAC/Multiplayer Scene List")]
    public class MultiplayerSceneListSO : ScriptableObject
    {
        public List<SceneEntry> scenes;

        [System.Serializable]
        public class SceneEntry
        {
            public bool includeInBuild;
            public bool clientBuild;
            public string developmentFusionRegion;
            public string clientFusionRegion;
            public string packageName;
            public string productName;
            public SceneAsset[] sceneAssets;
        }
    }
}