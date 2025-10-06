using DG.Tweening;
using TMPro;
using UnityEngine;

namespace PTTI_Multiplayer
{
    public class EnterRoomPassword_Panel : Panel
    {
        [SerializeField] string roomName = "1234";
        [SerializeField] string roomPassword = "1234";
        [SerializeField] TMP_InputField roomPasswordField;
        [SerializeField] TextMeshProUGUI errorTxt;

        public void ShowPanel(string roomName, string roomPassword)
        {
            Open();

            this.roomName = roomName;
            this.roomPassword = roomPassword;
        }

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);

            errorTxt.text = string.Empty;
            roomPasswordField.text = string.Empty;

#if UNITY_EDITOR
            roomPasswordField.text = "1234";
#endif
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void JoinRoomClick()
        {
            if (string.IsNullOrEmpty(roomPasswordField.text))
            {
                errorTxt.text = "Please enter your room password.";
                return;
            }

            if (!roomPasswordField.text.Equals(roomPassword))
            {
                errorTxt.text = "Please enter your correct room password.";
                return;
            }

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            errorTxt.text = string.Empty;

            Close();

            Multiplayer_UIManager.Instance.joinRoom_Panel.Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            Loading_Panel.instance.ShowLoading("Joining room, please wait a moment.");

            FusionLobbyManager.Instance.roomStatus = RoomStatus.Joining;

            Multiplayer_UIManager.Instance.joinRoom_Panel.Close();

            DOVirtual.DelayedCall(0.75f, () =>
            {
                FusionLobbyManager.Instance.CreateOrJoinRoom(roomName);
            });
        }

        public void BackClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();
        }
    }
}