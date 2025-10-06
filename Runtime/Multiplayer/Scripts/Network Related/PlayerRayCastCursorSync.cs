using Fusion;
using Meta.XR.MultiplayerBlocks.Fusion;
using PTTI_Multiplayer;
using UnityEngine;

public class PlayerRayCastCursorSync : NetworkBehaviour
{
    [Header("Visual objects in prefab")]
    [SerializeField] Custom_ControllerRayVisual leftRay;
    [SerializeField] Custom_ControllerRayVisual rightRay;
    [SerializeField] Custom_RayInteractorCursorVisual leftCursor;
    [SerializeField] Custom_RayInteractorCursorVisual rightCursor;

    public void AssignRaycastReferences()
    {
        if (FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher)
        {
            leftRay._rayInteractor = NetworkPlayerSpawner.instance.left_localRayInteractorRef;
            rightRay._rayInteractor = NetworkPlayerSpawner.instance.right_localRayInteractorRef;

            leftCursor._rayInteractor = NetworkPlayerSpawner.instance.left_localRayInteractorRef;
            rightCursor._rayInteractor = NetworkPlayerSpawner.instance.right_localRayInteractorRef;

            leftCursor._playerHead = NetworkPlayerSpawner.instance.leftCenterEyeTransform;
            rightCursor._playerHead = NetworkPlayerSpawner.instance.rightCenterEyeTransform;

            leftCursor.enabled = true;
            rightCursor.enabled = true;

            leftRay.enabled = true;
            rightRay.enabled = true;
        }
        else
        {
            leftRay.transform.localScale = Vector2.zero;
            rightRay.transform.localScale = Vector2.zero;

            leftCursor.transform.localScale = Vector2.zero;
            rightCursor.transform.localScale = Vector2.zero;
        }
    }
}