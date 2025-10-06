using DG.Tweening;
using UnityEngine;

namespace PTTI_Multiplayer
{
    public class Teacher_Panel : Panel
    {
        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void JoinRoomClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            FusionLobbyManager.Instance.InitRunner();

            Multiplayer_UIManager.Instance.joinRoom_Panel.Open(0.5f);
        }

        public void CreateRoomClick()
        {
            Multiplayer_SoundManager.Instance.PlayClick();

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            Close();

            Loading_Panel.instance.ShowLoading("Creating room, please wait a moment.");

            FusionLobbyManager.Instance.roomStatus = RoomStatus.Creating;

            DOVirtual.DelayedCall(0.75f, () =>
            {
#if UNITY_EDITOR
                FusionLobbyManager.Instance.CreateOrJoinRoom("123456");
#else
                FusionLobbyManager.Instance.CreateOrJoinRoom(Random.Range(000000, 999999).ToString());
#endif
            });
        }

        public void BackClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            Multiplayer_UIManager.Instance.selectAvatar_Panel.Open(0.5f);
        }
    }
}