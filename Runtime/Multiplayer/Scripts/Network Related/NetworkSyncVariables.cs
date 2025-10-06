using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Fusion;
using Fusion.Addons.Drawing;
using PTTI_Multiplayer;
using UnityEngine;

public class NetworkSyncVariables : NetworkBehaviour
{
    public static NetworkSyncVariables Instance { get; private set; }

    [Header("RoomTeacher Variables")]
    [Networked, OnChangedRender(nameof(OnPassthroughChanged))]
    public bool IsPassthroughEnabled { get; set; }

    [Header("RoomObjects to Sync")]
    [SerializeField] private List<GameObject> objectsToSync; // Assign in inspector

    [Networked, Capacity(30), OnChangedRender(nameof(OnStatesChanged))]
    private NetworkArray<bool> ActiveStates { get; }

    public List<GameObject> preDisabledList;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FusionLobbyManager.Instance.InsideStep();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            for (int i = 0; i < preDisabledList.Count; i++)
            {
                preDisabledList[i].SetActive(false);
            }
        }

        ActiveObject();

        if (IsPassthroughEnabled)
        {
            OnPassthroughChanged();
        }
    }

    #region ActiveDeactiveGameObjects
    public void ActiveObject()
    {
        // On first spawn, set initial states (only on state authority)
        if (Object.HasStateAuthority)
        {
            for (int i = 0; i < objectsToSync.Count; i++)
            {
                ActiveStates.Set(i, objectsToSync[i].activeSelf);
            }
        }
        else
        {
            for (int i = 0; i < objectsToSync.Count; i++)
            {
                objectsToSync[i].SetActive(false);
            }
            DOVirtual.DelayedCall(0.1f, () =>
            {
                OnStatesChanged();
            });
        }
    }

    private void OnPassthroughChanged()
    {
        Multiplayer_UIManager.Instance.menu_Panel.UpdatePassthroughToggle(IsPassthroughEnabled);
        FusionLobbyManager.Instance.ActivePassthrough(IsPassthroughEnabled);
    }

    private void OnStatesChanged()
    {
        for (int i = 0; i < objectsToSync.Count; i++)
        {
            objectsToSync[i].SetActive(ActiveStates.Get(i));
        }
    }

    public void SetObjectActive(GameObject target, bool active)
    {
        StartCoroutine(SetObjCoroutine(target, active));
    }

    private IEnumerator SetObjCoroutine(GameObject target, bool active)
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
            while (!Object.HasStateAuthority)
            {
                yield return null;
            }
        }

        yield return null;

        int index = objectsToSync.IndexOf(target);
        if (index == -1)
        {
            yield return null;
        }
        ActiveStates.Set(index, active);
    }

    public void SetPassthrough()
    {
        StartCoroutine(Set_Passthrough());
    }

    private IEnumerator Set_Passthrough()
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
            while (!Object.HasStateAuthority)
            {
                yield return null;
            }
        }

        yield return null;

        IsPassthroughEnabled = !IsPassthroughEnabled;
    }

    #endregion

    #region GhostHandLogic
    public void ActiveLeftHand(bool active)
    {
        StartCoroutine(SetHand(active, true));
    }

    public void ActiveRightHand(bool active)
    {
        StartCoroutine(SetHand(active, false));
    }

    public void ActivateHands()
    {
        StartCoroutine(SetHand(true, true));
        StartCoroutine(SetHand(true, false));
    }

    private IEnumerator SetHand(bool active, bool isLeft)
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
            while (!Object.HasStateAuthority)
            {
                //Debug.Log("Still not Authority");
                yield return null;
            }
        }

        yield return null;

        if (FusionLobbyManager.Instance.networkRig != null && isLeft)
        {
            FusionLobbyManager.Instance.networkRig.leftHand.transform.localScale = active ? Vector3.one : Vector3.zero;
        }
        else if (FusionLobbyManager.Instance.networkRig != null && !isLeft)
        {
            FusionLobbyManager.Instance.networkRig.rightHand.transform.localScale = active ? Vector3.one : Vector3.zero;
        }
    }

    #endregion

    #region TeacherCallsRPC
    public void SendPermissionToAllPlayers(bool isGrabAll)
    {
        //Debug.Log("MuteAllPlayers called");

        List<int> playerIds = new();

        var activePlayers = FusionLobbyManager.Instance._runner.ActivePlayers.ToList();

        for (int i = 0; i < activePlayers.Count; i++)
        {
            if (activePlayers[i].PlayerId != FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId)
            {
                // Your logic here
                playerIds.Add(activePlayers[i].PlayerId);
            }
        }

        RPC_GrabAllPlayers(playerIds.ToArray(), isGrabAll);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_GrabAllPlayers(int[] playerIds, bool permission, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_MutePlayer - " + playerIds.Length + " : " + mute);

        if (playerIds.Contains(FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId))
        {
            FusionLobbyManager.Instance.OnPermissionChanged(permission);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendPermissionToPlayer(int playerId, bool permission, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_SendPermissionToPlayer - " + playerId + " : " + permission);

        if (FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId.Equals(playerId))
        {
            FusionLobbyManager.Instance.OnPermissionChanged(permission);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ExitSession(RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.Log("RPC_ExitSession called by " + info.Source);

        ExitSession();
    }

    void ExitSession()
    {
        FusionLobbyManager.Instance.roomStatus = RoomStatus.EndSession;

        Loading_Panel.instance.ShowLoading("Leaving room, please wait a moment.");

        DOVirtual.DelayedCall(0.75f, () =>
        {
            FusionLobbyManager.Instance.ShutDownRunner();
        });
    }

    public void MicOnAllPlayers(bool isMicOnAll)
    {
        //Debug.Log("MicOnAllPlayers called");

        List<int> playerIds = new();

        var activePlayers = FusionLobbyManager.Instance._runner.ActivePlayers.ToList();

        for (int i = 0; i < activePlayers.Count; i++)
        {
            if (activePlayers[i].PlayerId != FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId)
            {
                // Your logic here
                playerIds.Add(activePlayers[i].PlayerId);
            }
        }

        RPC_MicOnAllPlayers(playerIds.ToArray(), isMicOnAll);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_MicOnAllPlayers(int[] playerIds, bool micOn, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_MicOnAllPlayers - " + playerIds.Length + " : " + micOn);

        if (playerIds.Contains(FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId))
        {
            FusionLobbyManager.Instance.OnMicChanged(micOn);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_MicOnPlayer(int playerId, bool micOn, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        Debug.LogError("RPC_MicOnPlayer - " + playerId + " : " + micOn);

        if (FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId.Equals(playerId))
        {
            FusionLobbyManager.Instance.OnMicChanged(micOn);
        }
    }

    public void LockAllPlayers(bool isLockAll)
    {
        //Debug.Log("LockAllPlayers called");

        List<int> playerIds = new();

        var activePlayers = FusionLobbyManager.Instance._runner.ActivePlayers.ToList();

        for (int i = 0; i < activePlayers.Count; i++)
        {
            if (activePlayers[i].PlayerId != FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId)
            {
                // Your logic here
                playerIds.Add(activePlayers[i].PlayerId);
            }
        }

        RPC_LockAllPlayers(playerIds.ToArray(), isLockAll);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_LockAllPlayers(int[] playerIds, bool lockMovement, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_LockAllPlayers - " + playerIds.Length + " : " + lockMovement);

        if (playerIds.Contains(FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId))
        {
            FusionLobbyManager.Instance.OnMovementChanged(lockMovement);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_LockMovementPlayer(int playerId, bool permission, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_SendPermissionToPlayer - " + playerId + " : " + permission);

        if (FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId.Equals(playerId))
        {
            FusionLobbyManager.Instance.OnMovementChanged(permission);
        }
    }

    public void ResetAllPlayers()
    {
        //Debug.Log("ResetAllPlayers called");

        RPC_ResetAllPlayers();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ResetAllPlayers(RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_ResetAllPlayers");

        FusionLobbyManager.Instance.ResetLocalPlayer();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ResetPlayer(int playerId, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_ResetPlayer - " + playerId);

        if (FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId == playerId)
        {
            FusionLobbyManager.Instance.ResetLocalPlayer();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_KickPlayer(int playerId, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_KickPlayer - " + playerId);

        if (FusionLobbyManager.Instance._runner.LocalPlayer.PlayerId == playerId)
        {
            ExitSession();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ClearBoard(RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_ClearBoard");

        Board.Instance.ClearBoard();
    }
    #endregion
}