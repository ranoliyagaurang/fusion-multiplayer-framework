using System.Collections.Generic;
using DG.Tweening;
using Fusion.Addons.Avatar;
using Fusion.Addons.Avatar.ReadyPlayerMe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PTTI_Multiplayer
{
    public class SelectAvatar_Panel : Panel
    {
        [SerializeField] RPMAvatarLibrary library;
        [SerializeField] List<RPMAvatarLoader.RPMCachedAvatarInfo> avatars;
        [SerializeField] UI_RPMChoiceButton rPMChoicePrefab;
        [SerializeField] List<UI_RPMChoiceButton> choiceButtons = new();
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] ToggleGroup toggleGroup;
        [SerializeField] TMP_InputField nameField;
        [SerializeField] TextMeshProUGUI errorTxt;
        [SerializeField] Transform avatarModel;
        [SerializeField] Vector3 defaultAvatarScale;
        public AvatarRepresentation avatarRepresentation;

        void Start()
        {
            avatarModel.transform.localScale = Vector3.zero;
            avatarModel.gameObject.SetActive(false);
        }

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);

            errorTxt.text = string.Empty;
            nameField.text = string.Empty;

            avatarModel.gameObject.SetActive(true);
            avatarModel.transform.DOScale(defaultAvatarScale, 0.35f).SetEase(Ease.OutBack);

#if UNITY_EDITOR
            nameField.text = "Manas";
#endif

            GenerateAvatars();
        }

        void GenerateAvatars()
        {
            if (FusionLobbyManager.Instance.playerMode == PlayerMode.Student)
                avatars = library.cachedAvatars.FindAll(x => !x.isTeacher);
            else
                avatars = library.cachedAvatars.FindAll(x => x.isTeacher);

            for (int i = 0; i < choiceButtons.Count; i++)
            {
                choiceButtons[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < avatars.Count; i++)
            {
                if (choiceButtons.Count <= i)
                {
                    choiceButtons.Add(Instantiate(rPMChoicePrefab, scrollRect.content));
                }

                choiceButtons[i].Bind(avatars[i].avatarURL, toggleGroup);
            }

            choiceButtons[0].ToggleOn();
            choiceButtons[0].OnClick();
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);

            DOVirtual.DelayedCall(delay, () =>
            {
                for (int i = 0; i < choiceButtons.Count; i++)
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            });

            avatarModel.transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.Linear).OnComplete(() =>
            {
                avatarModel.gameObject.SetActive(false);
            });
        }

        public void SubmitClick()
        {
            if (string.IsNullOrEmpty(nameField.text))
            {
                errorTxt.text = "Please enter your name.";
                return;
            }

            errorTxt.text = string.Empty;

            Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            FusionLobbyManager.Instance.playerName = nameField.text;

            if (FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher)
            {
                Multiplayer_UIManager.Instance.teacher_Panel.Open(0.5f);
            }
            else
            {
                FusionLobbyManager.Instance.InitRunner();
                Multiplayer_UIManager.Instance.joinRoom_Panel.Open(0.5f);
            }
        }

        public void BackClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            FusionLobbyManager.Instance.SetClass(string.Empty);

            Multiplayer_UIManager.Instance.selectClass_Panel.Open(0.5f);
        }
    }
}