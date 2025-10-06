using TMPro;
using UnityEngine;

namespace PTTI_Multiplayer
{
    public class Loading_Panel : Panel
    {
        public static Loading_Panel instance;
        public TextMeshProUGUI loadingTxt;

        void Start()
        {
            instance = this;
        }

        public void ShowLoading(string text)
        {
            loadingTxt.text = text;

            Open(0.5f);
        }

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }
    }
}