using DG.Tweening;
using PTTI_Multiplayer;
using TMPro;
using UnityEngine;

public class RoomView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI roomNameTxt;
    [SerializeField] string roomName;
    [SerializeField] string password;

    public void Bind(string roomName, string password = "")
    {
        this.roomName = roomName;

        this.password = password;

        roomNameTxt.text = roomName;

        gameObject.SetActive(true);
    }

    public void JoinRoomClick()
    {
        Multiplayer_SoundManager.Instance.PlayClick();

        if (!string.IsNullOrEmpty(password))
        {
            Multiplayer_UIManager.Instance.enterRoomPassword_Panel.ShowPanel(roomName, password);
            return;
        }

        if (!Multiplayer_UIManager.Instance.IsInternetAvailable())
        {
            return;
        }

        Loading_Panel.instance.ShowLoading("Joining room, please wait a moment.");

        FusionLobbyManager.Instance.roomStatus = RoomStatus.Joining;

        Multiplayer_UIManager.Instance.joinRoom_Panel.Close();

        DOVirtual.DelayedCall(0.75f, () =>
        {
            FusionLobbyManager.Instance.CreateOrJoinRoom(roomName);
        });
    }
}