using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI pingText;
    [SerializeField] Image micImg;
    [SerializeField] Sprite[] micSprites;
    [SerializeField] GameObject cannotGrab;
    [SerializeField] GameObject canGrab;
    [SerializeField] GameObject playerIsLock;
    [SerializeField] GameObject playerCanMove;
    [SerializeField] GameObject avatarIsHide;
    [SerializeField] GameObject avatarIsVisible;
    [SerializeField] GameObject kickingPlayerMsg;
    [SerializeField] GameObject cantAccessMsg;
    public PlayerData player;

    public void Off()
    {
        gameObject.SetActive(false);
    }

    public bool IsLockMovement()
    {
        if (gameObject.activeSelf)
        {
            return player.data.LockMovement;
        }

        return true;
    }

    public bool IsMicOn()
    {
        if (gameObject.activeSelf)
        {
            return player.data.IsMicOn;
        }

        return true;
    }

    public bool IsHidePlayer()
    {
        if (gameObject.activeSelf)
        {
            return player.data.HidePlayer;
        }

        return true;
    }

    public bool HasGrabPermission()
    {
        if (gameObject.activeSelf)
        {
            return player.data.GrabPermission;
        }

        return true;
    }

    public bool IsStudent()
    {
        if (gameObject.activeSelf)
        {
            return player.data.PlayerMode == PlayerMode.Student;
        }

        return false;
    }

    public void Bind(PlayerData player)
    {
        nameText.text = (transform.GetSiblingIndex() + 1).ToString() + ". " + player.playerName + " [" + player.playerMode + "]";

        this.player = player;

        gameObject.SetActive(true);

        cannotGrab.SetActive(!player.data.GrabPermission);

        canGrab.SetActive(player.data.GrabPermission);

        playerIsLock.SetActive(player.data.LockMovement);

        playerCanMove.SetActive(!player.data.LockMovement);

        avatarIsHide.SetActive(player.data.HidePlayer);

        avatarIsVisible.SetActive(!player.data.HidePlayer);

        micImg.sprite = player.data.IsMicOn ? micSprites[0] : micSprites[1];

        kickingPlayerMsg.SetActive(false);

        cantAccessMsg.SetActive(FusionLobbyManager.Instance.playerMode == PlayerMode.Supervisor || player.playerMode == PlayerMode.Teacher);
    }

    public void UpdateAllToggles()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (player.data.PlayerMode != PlayerMode.Student)
        {
            return;
        }

        cannotGrab.SetActive(!player.data.GrabPermission);

        canGrab.SetActive(player.data.GrabPermission);

        playerIsLock.SetActive(player.data.LockMovement);

        playerCanMove.SetActive(!player.data.LockMovement);

        avatarIsHide.SetActive(player.data.HidePlayer);

        avatarIsVisible.SetActive(!player.data.HidePlayer);

        micImg.sprite = player.data.IsMicOn ? micSprites[0] : micSprites[1];
    }

    public void UpdateMicToggle(bool isMicOnAll)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (player.data.PlayerMode != PlayerMode.Student)
        {
            return;
        }

        micImg.sprite = isMicOnAll ? micSprites[0] : micSprites[1];
    }

    public void UpdateGrabToggle(bool isGrabAll)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (player.data.PlayerMode != PlayerMode.Student)
        {
            return;
        }

        cannotGrab.SetActive(!isGrabAll);

        canGrab.SetActive(isGrabAll);
    }

    public void UpdateHideToggle(bool isHideAll)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (player.data.PlayerMode != PlayerMode.Student)
        {
            return;
        }

        avatarIsHide.SetActive(!isHideAll);

        avatarIsVisible.SetActive(isHideAll);
    }

    public void UpdateLockToggle(bool isLockAll)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (player.data.PlayerMode != PlayerMode.Student)
        {
            return;
        }
        
        playerIsLock.SetActive(isLockAll);

        playerCanMove.SetActive(!isLockAll);
    }

    public void UpdatePingAndMic()
    {
        if (player.data != null)
        {
            pingText.text = $"{player.data.RttMs} ms";
        }
    }

    public void GrantPermissionClick()
    {
        NetworkSyncVariables.Instance.RPC_SendPermissionToPlayer(player.playerId, true);

        cannotGrab.SetActive(false);

        canGrab.SetActive(true);

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    public void RevokePermissionClick()
    {
        NetworkSyncVariables.Instance.RPC_SendPermissionToPlayer(player.playerId, false);

        cannotGrab.SetActive(true);

        canGrab.SetActive(false);

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    public void LockMovementClick()
    {
        NetworkSyncVariables.Instance.RPC_LockMovementPlayer(player.playerId, true);

        playerIsLock.SetActive(true);

        playerCanMove.SetActive(false);

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    public void UnLockMovementClick()
    {
        NetworkSyncVariables.Instance.RPC_LockMovementPlayer(player.playerId, false);

        playerIsLock.SetActive(false);

        playerCanMove.SetActive(true);

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    public void HideAvatarClick()
    {
        player.data.OnHidePlayerChange(true);

        avatarIsHide.SetActive(true);

        avatarIsVisible.SetActive(false);

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    public void ShowAvatarClick()
    {
        player.data.OnHidePlayerChange(false);

        avatarIsHide.SetActive(false);

        avatarIsVisible.SetActive(true);

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    public void MicChanged()
    {
        if (player.data != null)
        {
            micImg.sprite = micImg.sprite == micSprites[1] ? micSprites[0] : micSprites[1];

            NetworkSyncVariables.Instance.RPC_MicOnPlayer(player.playerId, micImg.sprite == micSprites[0]);
        }
    }

    public void ResetPlayerClick()
    {
        if (player.data != null)
        {
            NetworkSyncVariables.Instance.RPC_ResetPlayer(player.playerId);
        }

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    public void KickPlayerClick()
    {
        if (player.data != null)
        {
            NetworkSyncVariables.Instance.RPC_KickPlayer(player.playerId);

            kickingPlayerMsg.SetActive(true);
        }

        Multiplayer_SoundManager.Instance.PlayClick();
    }
}