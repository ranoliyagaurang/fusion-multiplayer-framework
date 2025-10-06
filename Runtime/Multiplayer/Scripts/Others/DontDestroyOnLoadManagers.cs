using UnityEngine;

namespace PTTI_Multiplayer
{
    public class DontDestroyOnLoadManagers : MonoBehaviour
    {
        public static DontDestroyOnLoadManagers Instance;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                DestroyImmediate(gameObject);
        }
    }
}