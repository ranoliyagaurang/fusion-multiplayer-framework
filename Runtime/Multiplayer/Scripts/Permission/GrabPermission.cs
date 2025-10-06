using PTTI.XR.MultiplayerBlocks.Shared;
using UnityEngine;
using Fusion;
using Oculus.Interaction.HandGrab;

[RequireComponent(typeof(TransferOwnershipOnSelect))]
public class GrabPermission : NetworkBehaviour
{
    [Header("NetworkProperties")]
    [Networked, OnChangedRender(nameof(OnPlaced))]
    public bool IsPlaced { get; set; }

    public bool canGrab = true;

    HandGrabInteractable[] handGrabInteractables = new HandGrabInteractable[0];
    TransferOwnershipOnSelect transferOwnershipOnSelect;

    bool IsGrabble
    {
        get
        {
            if (FusionLobbyManager.Instance.localPlayerData == null) return false;
            return FusionLobbyManager.Instance.localPlayerData.GrabPermission && canGrab;
        }
    }

    void OnPlaced()
    {
        if (handGrabInteractables.Length != 0)
        {
            foreach (HandGrabInteractable interactable in handGrabInteractables)
                interactable.MaxSelectingInteractors = 0;
        }

        if (transferOwnershipOnSelect != null)
        {
            transferOwnershipOnSelect.enabled = false;
        }
    }

    private void Awake()
    {
        if (handGrabInteractables.Length == 0)
        {
            handGrabInteractables = GetComponentsInChildren<HandGrabInteractable>();
        }

        if (transferOwnershipOnSelect == null)
        {
            transferOwnershipOnSelect = GetComponent<TransferOwnershipOnSelect>();
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        FusionLobbyManager.PermissionGranted += OnPermissionChanged;

        if (IsPlaced)
        {
            OnPlaced();
            return;
        }

        if (handGrabInteractables.Length != 0 && FusionLobbyManager.Instance.localPlayerData != null)
        {
            foreach (HandGrabInteractable interactable in handGrabInteractables)
                interactable.MaxSelectingInteractors = IsGrabble ? -1 : 0;
        }

        if (transferOwnershipOnSelect != null && FusionLobbyManager.Instance.localPlayerData != null)
        {
            transferOwnershipOnSelect.enabled = IsGrabble;
        }
    }

    void OnDestroy()
    {
        FusionLobbyManager.PermissionGranted -= OnPermissionChanged;
    }

    void OnDisable()
    {
        FusionLobbyManager.PermissionGranted -= OnPermissionChanged;
    }

    void OnPermissionChanged(bool granted)
    {
        if (IsPlaced) return;

        if (handGrabInteractables.Length != 0 && FusionLobbyManager.Instance.localPlayerData != null)
        {
            foreach (HandGrabInteractable interactable in handGrabInteractables)
                interactable.MaxSelectingInteractors = IsGrabble ? -1 : 0;
        }

        if (transferOwnershipOnSelect != null)
        {
            transferOwnershipOnSelect.enabled = IsGrabble;
        }
    }

    public void CanGrabObject(bool can)
    {
        canGrab = can;

        OnPermissionChanged(false);
    }
}