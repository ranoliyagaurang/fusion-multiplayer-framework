using Oculus.Interaction.Locomotion;
using UnityEngine;

[RequireComponent(typeof(TeleportInteractable))]
public class LockTeleport : MonoBehaviour
{
    TeleportInteractable teleportInteractable;

    void Awake()
    {
        teleportInteractable = GetComponent<TeleportInteractable>();
    }

    void OnEnable()
    {
        FusionLobbyManager.LockTeleport += OnLockTeleport;
        ControllerButtonsManager.RightSelected += OnSelected;

        if (teleportInteractable != null && FusionLobbyManager.Instance.localPlayerData != null)
        {
            teleportInteractable.AllowTeleport = !FusionLobbyManager.Instance.localPlayerData.LockMovement;
        }
    }

    void OnDisable()
    {
        FusionLobbyManager.LockTeleport -= OnLockTeleport;
        ControllerButtonsManager.RightSelected -= OnSelected;
    }

    private void OnLockTeleport(bool granted)
    {
        if (teleportInteractable != null)
            teleportInteractable.AllowTeleport = !granted;
    }

    private void OnSelected(Transform pointer)
    {
        if (pointer == null)
        {
            if (teleportInteractable != null && FusionLobbyManager.Instance.localPlayerData != null)
            {
                teleportInteractable.AllowTeleport = !FusionLobbyManager.Instance.localPlayerData.LockMovement;
            }
        }
        else
        {
            if (teleportInteractable != null)
                teleportInteractable.AllowTeleport = false;
        }
    }
}