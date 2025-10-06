using UnityEngine;

namespace PTTI_Multiplayer
{
    public class SelectClass_Panel : Panel
    {
        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void Class_AClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.SetClass("Class-A");

            Multiplayer_UIManager.Instance.selectAvatar_Panel.Open(0.5f);
        }

        public void Class_BClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.SetClass("Class-B");

            Multiplayer_UIManager.Instance.selectAvatar_Panel.Open(0.5f);
        }

        public void Class_CClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.SetClass("Class-C");

            Multiplayer_UIManager.Instance.selectAvatar_Panel.Open(0.5f);
        }

        public void BackClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            FusionLobbyManager.Instance.SetClass(string.Empty);

            Multiplayer_UIManager.Instance.selectRole_Panel.Open(0.5f);
        }
    }
}