using System;
using TMPro;
using UnityEngine;

namespace PTTI_Multiplayer
{
    public class Message_Panel : Panel
    {
        [SerializeField] TextMeshProUGUI msgTxt;
        Action callback;

        public void ShowPanel(float delay = 0, string msg = "", Action action = null)
        {
            callback = action;

            Open(delay, msg);
        }

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);

            msgTxt.text = msg;
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void OkayClick()
        {
            base.Close();

            callback?.Invoke();

            Multiplayer_SoundManager.Instance.PlayClick();
        }
    }
}