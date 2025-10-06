using PTTI.XR.MultiplayerBlocks.Shared;
using Oculus.Interaction;
using UnityEngine;
using Fusion;

[RequireComponent(typeof(TransferOwnershipOnSelectForPoke))]
public class PokePermission : NetworkBehaviour
{
    [Header("NetworkProperties")]
    [Networked, OnChangedRender(nameof(OnPlaced))]
    public bool IsPlaced { get; set; }

    public bool canGrab = true;

    PokeInteractable[] pokeInteractables = new PokeInteractable[0];
    TransferOwnershipOnSelectForPoke transferOwnershipOnSelectForPoke;

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
        if (pokeInteractables.Length != 0 && FusionLobbyManager.Instance.localPlayerData != null)
        {
            foreach (PokeInteractable interactable in pokeInteractables)
                interactable.MaxSelectingInteractors = 0;
        }

        if (transferOwnershipOnSelectForPoke != null)
        {
            transferOwnershipOnSelectForPoke.enabled = false;
        }
    }

    private void Awake()
    {
        if (pokeInteractables.Length == 0)
        {
            pokeInteractables = GetComponentsInChildren<PokeInteractable>();
        }

        if (transferOwnershipOnSelectForPoke == null)
        {
            transferOwnershipOnSelectForPoke = GetComponent<TransferOwnershipOnSelectForPoke>();
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

        if (pokeInteractables.Length != 0 && FusionLobbyManager.Instance.localPlayerData != null)
        {
            foreach (PokeInteractable interactable in pokeInteractables)
                interactable.MaxSelectingInteractors = IsGrabble ? -1 : 0;
        }

        if (transferOwnershipOnSelectForPoke != null && FusionLobbyManager.Instance.localPlayerData != null)
        {
            transferOwnershipOnSelectForPoke.enabled = IsGrabble;
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

        if (pokeInteractables.Length != 0 && FusionLobbyManager.Instance.localPlayerData != null)
        {
            foreach (PokeInteractable pokeInteractable in pokeInteractables)
                pokeInteractable.MaxSelectingInteractors = IsGrabble ? -1 : 0;
        }

        if (transferOwnershipOnSelectForPoke != null)
        {
            transferOwnershipOnSelectForPoke.enabled = IsGrabble;
        }
    }

    public void CanGrabObject(bool can)
    {
        canGrab = can;

        OnPermissionChanged(false);
    }
}