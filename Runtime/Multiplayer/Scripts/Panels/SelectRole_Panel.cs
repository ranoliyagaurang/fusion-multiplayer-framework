using UnityEngine;

namespace PTTI_Multiplayer
{
    public class SelectRole_Panel : Panel
    {
        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void TeacherClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.playerMode = PlayerMode.Teacher;

            Multiplayer_UIManager.Instance.enterPassword_Panel.Open(0.5f);
        }

        public void SupervisorClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.playerMode = PlayerMode.Supervisor;

            Multiplayer_UIManager.Instance.enterPassword_Panel.Open(0.5f);
        }

        public void StudentClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.playerMode = PlayerMode.Student;

            Multiplayer_UIManager.Instance.selectClass_Panel.Open(0.5f);
        }
    }
}