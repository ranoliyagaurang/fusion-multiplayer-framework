using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PTTI_Multiplayer
{
    public class EnterPassword_Panel : Panel
    {
        [SerializeField] string password = "1234";
        [SerializeField] TMP_InputField passwordField;
        [SerializeField] TMP_InputField roomPasswordField;
        [SerializeField] Toggle passwordToggle;
        [SerializeField] TextMeshProUGUI errorTxt;

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);

            errorTxt.text = string.Empty;
            passwordField.text = string.Empty;
            roomPasswordField.text = string.Empty;
            roomPasswordField.gameObject.SetActive(false);
            passwordToggle.isOn = false;

            passwordToggle.gameObject.SetActive(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher);

#if UNITY_EDITOR
            passwordField.text = password;
            roomPasswordField.text = "1234";
#endif
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void SubmitClick()
        {
            if (string.IsNullOrEmpty(passwordField.text))
            {
                errorTxt.text = "Please enter your password.";
                return;
            }

            if (!passwordField.text.Equals(password))
            {
                errorTxt.text = "Please enter your correct password.";
                return;
            }

            if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
            {
                return;
            }

            if (passwordToggle.isOn && string.IsNullOrEmpty(roomPasswordField.text))
            {
                errorTxt.text = "Please enter a room password.";
                return;
            }

            errorTxt.text = string.Empty;

            string roomPassword = passwordToggle.isOn ? roomPasswordField.text : string.Empty;

            Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            FusionLobbyManager.Instance.roomPassword = roomPassword;

            Multiplayer_UIManager.Instance.selectClass_Panel.Open(0.5f);
        }

        public void BackClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            Multiplayer_UIManager.Instance.selectRole_Panel.Open(0.5f);
        }
    }
}