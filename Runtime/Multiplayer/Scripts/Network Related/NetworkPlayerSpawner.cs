using Fusion;
using Fusion.XR.Shared.Rig;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.MultiplayerBlocks.Fusion
{
    public class NetworkPlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerNameTagPrefab;
        [SerializeField] Transform XRHeadTransform;
        [SerializeField] private Transform XRLeftHandTransform;
        [SerializeField] private Transform XRRightHandTransform;
        public Transform leftCenterEyeTransform;
        public Transform rightCenterEyeTransform;
        public RayInteractor left_localRayInteractorRef;
        public RayInteractor right_localRayInteractorRef;
        public HardwareHand left_localHardwareHandRef;
        public HardwareHand right_localHardwareHandRef;

        NetworkRunner _networkRunner;

        public static NetworkPlayerSpawner instance;

        void Awake()
        {
            instance = this;
        }

        private void OnEnable()
        {
            FusionBBEvents.OnSceneLoadDone += OnLoaded;
            FusionBBEvents.OnPlayerJoined += OnPlayerJoined;
        }

        private void OnDisable()
        {
            FusionBBEvents.OnSceneLoadDone -= OnLoaded;
            FusionBBEvents.OnPlayerJoined -= OnPlayerJoined;
        }

        private void OnLoaded(NetworkRunner networkRunner)
        {
            _networkRunner = networkRunner;
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef @ref)
        {
            // Only the shared-mode master client should spawn
            if (runner.LocalPlayer == @ref)
            {
                // This ensures only one instance spawns the player prefab
                var playerObj = _networkRunner.Spawn(playerNameTagPrefab, Vector3.zero, Quaternion.identity, _networkRunner.LocalPlayer);

                FusionLobbyManager.Instance.SpawnPlayerNetworkData(playerObj);
            }
        }
    }
}