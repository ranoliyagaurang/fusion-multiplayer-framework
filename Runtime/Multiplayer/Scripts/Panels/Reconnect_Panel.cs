using UnityEngine;

namespace PTTI_Multiplayer
{
    public class Reconnect_Panel : Panel
    {
        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void YesClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.Reconnect();
        }

        public void NoClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();
        }
    }
}