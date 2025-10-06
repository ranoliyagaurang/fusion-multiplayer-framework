using Fusion.Addons.Avatar.ReadyPlayerMe;
using UnityEngine;
using UnityEngine.UI;

public class UI_RPMChoiceButton : MonoBehaviour
{
    [SerializeField] Toggle toggle;
    [SerializeField] RPMAvatarLoader rPMAvatarLoader;

    public void Bind(string avatarURL, ToggleGroup group)
    {
        toggle.group = group;

        gameObject.SetActive(true);

        rPMAvatarLoader.startingAvatarUrl = avatarURL;
        rPMAvatarLoader.ChangeAvatar(avatarURL);
    }

    public void ToggleOn()
    {
        toggle.isOn = true;
    }

    public void OnClick()
    {
        Multiplayer_SoundManager.Instance.PlayClick();

        Multiplayer_UIManager.Instance.selectAvatar_Panel.avatarRepresentation.ChangeAvatar(rPMAvatarLoader.startingAvatarUrl);
        FusionLobbyManager.Instance.avatarURL = rPMAvatarLoader.startingAvatarUrl;
    }
}