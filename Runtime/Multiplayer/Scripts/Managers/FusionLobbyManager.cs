using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Fusion.XR.Shared.Rig;
using PTTI_Multiplayer;
using Meta.XR.MultiplayerBlocks.Fusion;
using Photon.Voice.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

// Defines player roles in the multiplayer session
public enum PlayerMode { Teacher, Student, Supervisor };

// Defines room/session states for better flow control
public enum RoomStatus { None, InLobby, Creating, Joining, InRoom, Disconnecting, EndSession }

/// <summary>
/// FusionLobbyManager handles lobby management, room creation/joining, 
/// player registration, permissions, mic handling, and reconnections.
/// Integrated with Fusion and Photon.
/// </summary>
public class FusionLobbyManager : MonoBehaviour
{
    // Event delegates for permissions and teleport locking
    public delegate void OnPermissionGranted(bool granted);
    public static event OnPermissionGranted PermissionGranted;

    public delegate void OnLockTeleport(bool granted);
    public static event OnLockTeleport LockTeleport;

    public delegate void OnResetPlayer();
    public static event OnResetPlayer ResetPlayer;

    public delegate void OnHideAvatar(bool hide);
    public static event OnHideAvatar HideAvatar;

    // Singleton instance
    public static FusionLobbyManager Instance;

    [Header("Managers")]
    [SerializeField] GameObject controllerButtonsManager;

    [Header("Player Transforms")]
    [SerializeField] Transform metaLocalPlayer; // Player root
    [SerializeField] Transform metaLocalPlayerTeleport; // Teleport helper
    [SerializeField] GameObject metaTurnerInteractor;
    [SerializeField] GameObject metaTeleportInteractor;

    [Header("Player Session Data")]
    public PlayerMode playerMode = PlayerMode.Teacher;
    public string className;
    public RoomStatus roomStatus;
    [SerializeField] string lobbyId;
    [SerializeField] string lobbySceneName;
    public string playerName;
    public string avatarURL;
    public string roomCode;
    public string roomPassword;
    public string fusionRegion;
    public string connectedFusionRegion;

    [Header("Voice")]
    // Voice recorder handling mic state
    public Recorder recorder;

    [Header("Networking")]
    // Fusion/Photon networking components
    [SerializeField] NetworkRunner runnerPrefab;
    public NetworkRunner _runner;
    public NetworkRig networkRig;
    public GameObject passthroughObj;
    public PlayerNetworkData localPlayerData;
    public bool isReconnecting;
    public bool isResetData;

    [Header("Player Tracking")]
    // List of connected players (tracked locally)
    [SerializeField] List<PlayerData> players = new();

    [Header("UI References")]
    // Reference to player UI watch
    [SerializeField] GameObject watchObj;

    [Header("Photon Settings")]
    // Photon settings (versioning)
    [SerializeField] PhotonAppSettings appSettings;

    // Default values to reset local player
    Vector3 defaultPlayerPosition;
    Vector3 defaultPlayerRotation;

    // Reconnect values to reset local player
    Vector3 reconnectPlayerPosition;
    Vector3 reconnectPlayerRotation;
    bool reconnectGrabPermission;
    bool reconnectLockMovement;
    bool reconnectMicOn;
    bool reconnectLocalMicOn;

    #region UnityCallbacks
    private void Awake()
    {
        // Setup singleton
        Instance = this;

        // Assign Photon app version dynamically to avoid mismatched builds
        if (appSettings == null)
        {
            Debug.LogWarning("Assigned photon settings reference so app version can be assigned and change app version to not connect with clients build.");
        }
        else
        {
            appSettings.AppSettings.AppVersion = Application.version;
        }

        // Store default position and rotation for reset
        defaultPlayerPosition = metaLocalPlayer.position;
        defaultPlayerRotation = metaLocalPlayer.eulerAngles;
    }

    void Start()
    {
        Multiplayer_UIManager.Instance.menu_Panel.Open();

        if (appSettings.AppSettings.FixedRegion == "eu")
        {
            PlayfabManager.Instance.AnonymousLogin();
        }
    }

    private void OnEnable()
    {
        // Register Fusion events
        FusionBBEvents.OnConnectFailed += ConnectFailed;
        FusionBBEvents.OnShutdown += Shutdown;
        FusionBBEvents.OnPlayerJoined += OnPlayerJoined;
        FusionBBEvents.OnPlayerLeft += OnPlayerLeftEvent;
        FusionBBEvents.OnSceneLoadDone += OnLoaded;
        FusionBBEvents.OnSessionListUpdated += OnSessionListUpdated;
        FusionBBEvents.OnConnectedToServer += OnConnectedToServer;
    }

    private void OnDisable()
    {
        // Unregister Fusion events
        FusionBBEvents.OnPlayerJoined -= OnPlayerJoined;
        FusionBBEvents.OnPlayerLeft -= OnPlayerLeftEvent;
        FusionBBEvents.OnSceneLoadDone -= OnLoaded;
        FusionBBEvents.OnSessionListUpdated -= OnSessionListUpdated;
        FusionBBEvents.OnConnectFailed -= ConnectFailed;
        FusionBBEvents.OnShutdown -= Shutdown;
        FusionBBEvents.OnConnectedToServer -= OnConnectedToServer;
    }

    void OnApplicationFocus(bool focus)
    {
        // Toggle watch object visibility based on focus
        watchObj.SetActive(focus);
    }
    #endregion

    #region RegisteredEvents
    private void OnPlayerJoined(NetworkRunner runner, PlayerRef @ref)
    {
        // Teacher/Supervisor only track joins
        if (playerMode == PlayerMode.Student)
            return;
    }

    private void OnPlayerLeftEvent(NetworkRunner runner, PlayerRef @ref)
    {
        // Skip if student (not responsible for player list)
        if (playerMode == PlayerMode.Student)
            return;

        // Remove player from list and update UI
        int index = players.IndexOf(players.Find(x => x.playerId.Equals(@ref.PlayerId)));
        if (index != -1)
        {
            Multiplayer_UIManager.Instance.menu_Panel.ShowLog("[" + players[index].playerMode + "] " + players[index].playerName + " has leave the room.");
            players.RemoveAt(index);
        }

        Multiplayer_UIManager.Instance.menu_Panel.UpdatePlayers(players);
    }

    private void OnLoaded(NetworkRunner networkRunner)
    {
        // Show room code when scene load completes
        Multiplayer_UIManager.Instance.menu_Panel.ShowRoomCode(networkRunner.SessionInfo.Name);
    }

    private void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Update available sessions in UI
        Debug.Log($"[Fusion] OnSessionListUpdated: {sessionList.Count} rooms");
        Multiplayer_UIManager.Instance.joinRoom_Panel.SessionListUpdate(sessionList);

        if (runner.SessionInfo != null)
        {
            Debug.Log($"[Fusion] Connected to server in region: {runner.SessionInfo.Region}");
            connectedFusionRegion = runner.SessionInfo.Region;
            Multiplayer_UIManager.Instance.menu_Panel.UpdateRegion();
        }
    }

    private void OnConnectedToServer(NetworkRunner runner)
    {
        if (runner.SessionInfo != null)
        {
            Debug.Log($"[Fusion] Connected to server in region: {runner.SessionInfo.Region}");
            connectedFusionRegion = runner.SessionInfo.Region;
            Multiplayer_UIManager.Instance.menu_Panel.UpdateRegion();
        }
    }

    private void ConnectFailed(NetworkRunner runner, NetAddress address, NetConnectFailedReason failedReason)
    {
        // Handle connection failure depending on state
        switch (roomStatus)
        {
            case RoomStatus.Creating:
            case RoomStatus.Joining:
                Loading_Panel.instance.Close();
                Multiplayer_UIManager.Instance.message_Panel.ShowPanel(0.5f, "Disconnected by " + failedReason.ToString(), Multiplayer_UIManager.Instance.OpenPanelAfterDisconnect);
                break;
        }
    }

    private void Shutdown(NetworkRunner runner, ShutdownReason failedReason)
    {
        // Log shutdown reasons and reset UI accordingly
        Debug.Log($"[Fusion] Shutdown: {failedReason}");
        Debug.Log($"[Fusion] Room Status: {roomStatus}");

        switch (roomStatus)
        {
            case RoomStatus.Creating:
            case RoomStatus.Joining:
                Loading_Panel.instance.Close();
                Multiplayer_UIManager.Instance.message_Panel.ShowPanel(0.5f, "Disconnected by " + failedReason.ToString(), Multiplayer_UIManager.Instance.OpenPanelAfterDisconnect);
                break;

            case RoomStatus.InRoom:
                ResetUI();
                Multiplayer_UIManager.Instance.reconnect_Panel.Open(0.5f);
                break;

            case RoomStatus.Disconnecting:
                ResetUI();
                break;

            case RoomStatus.EndSession:
                ResetUI();
                Multiplayer_UIManager.Instance.message_Panel.ShowPanel(0.5f, "Teacher has ended the session.");
                break;
        }

        if (runner.SessionInfo != null)
        {
            Debug.Log($"[Fusion] Connected to server in region: {runner.SessionInfo.Region}");
            connectedFusionRegion = string.Empty;
            Multiplayer_UIManager.Instance.menu_Panel.UpdateRegion();
        }
    }
    #endregion

    #region PlayersPublicFunctions
    public bool IsNoTeacherPresentInRoom()
    {
        if (playerMode == PlayerMode.Student || playerMode == PlayerMode.Supervisor)
            return false;
        return players.Count(x => x.data.PlayerMode == PlayerMode.Teacher) == 0;
    }

    /// <summary>
    /// Initializes the network runner and joins a lobby.
    /// </summary>
    public async void InitRunner()
    {
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Please enter lobby id...");
            return;
        }

        if (string.IsNullOrEmpty(className))
        {
            Debug.LogError("Please select your class...");
            return;
        }

        roomStatus = RoomStatus.InLobby;

        _runner = Instantiate(runnerPrefab);
        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(2);

        if (!scene.IsValid)
        {
            Debug.LogError("[Fusion] Invalid scene index for multiplayer!");
            return;
        }

        string scenePath = SceneUtility.GetScenePathByBuildIndex(2);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        string lobbyName = lobbyId + "_" + className + "_" + sceneName.Replace("_Multiplayer", "");

        // Create Photon Realtime settings
        var photonSettings = appSettings.AppSettings;

        if (!string.IsNullOrEmpty(fusionRegion))
            photonSettings.FixedRegion = fusionRegion; // 👈 Change region here (e.g. "us", "eu", "in")

        var result = await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyName, null, photonSettings);
        if (result.Ok)
        {
            Debug.Log("[Fusion] Joined lobby: " + lobbyName);
        }
        else
        {
            Debug.LogError($"[Fusion] Failed to join lobby!\n- ShutdownReason: {result.ShutdownReason}");
        }
    }

    /// <summary>
    /// Gracefully shuts down the network runner.
    /// </summary>
    public async void ShutDownRunner()
    {
        await _runner.Shutdown();
        Debug.Log("[Fusion] Runner Shutdown");
    }

    /// <summary>
    /// Create or join a specific room by name.
    /// </summary>
    public void CreateOrJoinRoom(string roomName)
    {
        roomCode = roomName;
        CreateOrJoinRoomOperation(roomName);
    }

    /// <summary>
    /// Register player when spawned.
    /// </summary>
    public void RegisterSpawnedPlayer(PlayerRef playerRef, NetworkObject obj)
    {
        if (playerMode == PlayerMode.Student)
            return;

        RegisteredPlayer(playerRef, obj);
    }

    /// <summary>
    /// Attempt to reconnect to the last room.
    /// </summary>
    public void Reconnect()
    {
        isReconnecting = true;
        isResetData = true;
        Multiplayer_UIManager.Instance.selectRole_Panel.Close();
        roomStatus = RoomStatus.Joining;
        Loading_Panel.instance.ShowLoading("Re-Joining room, please wait a moment.");
        CreateOrJoinRoomOperation(roomCode);
    }

    /// <summary>
    /// Toggle microphone state.
    /// </summary>
    public void Mic(bool active)
    {
        if (recorder != null)
        {
            recorder.RecordingEnabled = active;
            reconnectLocalMicOn = active;
        }
    }

    /// <summary>
    /// Update mute of mic.
    /// </summary>
    public void OnMicChanged(bool granted)
    {
        if (playerMode != PlayerMode.Student) return;
        
        reconnectMicOn = granted;

        recorder.TransmitEnabled = granted;

        localPlayerData.IsMicOn = granted;

        Multiplayer_UIManager.Instance.menu_Panel.UpdateStatus();
    }

    /// <summary>
    /// Update grab permission and notify listeners.
    /// </summary>
    public void OnPermissionChanged(bool granted)
    {
        if (playerMode != PlayerMode.Student) return;
        //Debug.Log("[Fusion] Grab permission granted.");
        localPlayerData.GrabPermission = granted;
        PermissionGranted?.Invoke(granted);

        reconnectGrabPermission = granted;

        Multiplayer_UIManager.Instance.menu_Panel.UpdateStatus();
    }

    /// <summary>
    /// Update movement/teleport permission and notify listeners.
    /// </summary>
    public void OnMovementChanged(bool granted)
    {
        if (playerMode != PlayerMode.Student) return;
        //Debug.Log("[Fusion] movement granted.");
        localPlayerData.LockMovement = granted;
        LockTeleport?.Invoke(granted);

        reconnectLockMovement = granted;

        Multiplayer_UIManager.Instance.menu_Panel.UpdateStatus();
    }

    /// <summary>
    /// Setup local player network data after spawning.
    /// </summary>
    public void SpawnPlayerNetworkData(NetworkObject playerObj)
    {
        recorder.TransmitEnabled = playerMode == PlayerMode.Teacher;
        recorder.RecordingEnabled = playerMode == PlayerMode.Teacher;

        networkRig = playerObj.GetComponent<NetworkRig>();
        localPlayerData = playerObj.GetComponent<PlayerNetworkData>();
        localPlayerData.PlayerName = playerName;
        localPlayerData.PlayerAvatarURL = avatarURL;

        if (isResetData)
        {
            localPlayerData.GrabPermission = reconnectGrabPermission;
            localPlayerData.LockMovement = reconnectLockMovement;
            localPlayerData.IsMicOn = reconnectMicOn;
            recorder.TransmitEnabled = reconnectMicOn;
            recorder.RecordingEnabled = reconnectLocalMicOn;

            isResetData = false;
        }
        else
        {
            localPlayerData.GrabPermission = playerMode == PlayerMode.Teacher;
            localPlayerData.LockMovement = playerMode == PlayerMode.Student;
            localPlayerData.IsMicOn = recorder.TransmitEnabled;

            reconnectGrabPermission = localPlayerData.GrabPermission;
            reconnectLockMovement = localPlayerData.LockMovement;
            reconnectMicOn = localPlayerData.IsMicOn;
            reconnectLocalMicOn = recorder.RecordingEnabled;
        }

        // Notify listeners of current state
        PermissionGranted?.Invoke(localPlayerData.GrabPermission);
        LockTeleport?.Invoke(localPlayerData.LockMovement);

        Multiplayer_UIManager.Instance.menu_Panel.localPlayerData = localPlayerData;

        controllerButtonsManager.SetActive(playerMode == PlayerMode.Teacher);

        playerObj.GetComponent<PlayerRayCastCursorSync>().AssignRaycastReferences();

        Multiplayer_UIManager.Instance.menu_Panel.UpdateStatus();
        Multiplayer_UIManager.Instance.menu_Panel.UpdateMicToggle();
    }

    /// <summary>
    /// Called once inside the room.
    /// </summary>
    public void InsideStep()
    {
        roomStatus = RoomStatus.InRoom;

        Multiplayer_UIManager.Instance.menu_Panel.Open();
    }

    /// <summary>
    /// Reset local player position/rotation to match target.
    /// </summary>
    public void RepositionMetaLocalPlayer(Transform target)
    {
        //Debug.Log($"RepositionMetaLocalPlayer to {target.position}, {target.eulerAngles}" + " - " + target.name);
        metaLocalPlayer.position = target.position;
        metaLocalPlayer.eulerAngles = target.eulerAngles;
        metaLocalPlayerTeleport.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Reset local player position/rotation by the teacher.
    /// </summary>
    public void ResetLocalPlayer()
    {
        //Debug.Log("ResetLocalPlayer");
        if(playerMode == PlayerMode.Student)
            ResetPlayer?.Invoke();
    }

    /// <summary>
    /// Reset local player when reconnecting to lobby.
    /// </summary>
    public void ResetLocalPlayerWhenReconnected()
    {
        metaLocalPlayer.position = reconnectPlayerPosition;
        metaLocalPlayer.eulerAngles = reconnectPlayerRotation;
        metaLocalPlayerTeleport.localPosition = Vector3.zero;

        isReconnecting = false;

        //Debug.Log("ResetLocalPlayerWhenReconnected");
    }

    /// <summary>
    /// Get local player position to reset player when reconnected.
    /// </summary>
    public void FetchLocalPlayerWhenTeleported()
    {
        if (roomStatus != RoomStatus.InRoom) return;
        reconnectPlayerPosition = metaLocalPlayerTeleport.position;
        reconnectPlayerRotation = metaLocalPlayerTeleport.eulerAngles;
        //Debug.Log("Fetch Reconnect Position - " + reconnectPlayerPosition + " : " + reconnectPlayerRotation);
    }

    public void StopLocomotion(bool active)
    {
        metaTurnerInteractor.SetActive(active);
        metaTeleportInteractor.SetActive(active);
    }

    public void ActivePassthrough(bool active)
    {
        passthroughObj.SetActive(active);

        if (PassthroughManager.Instance != null)
        {
            PassthroughManager.Instance.SetPassthrough(active);
        }
    }

    public void SetRegion(string region)
    {
        fusionRegion = region;
    }

    public void HidePlayerAvatar(bool hide)
    {
        HideAvatar?.Invoke(hide);
    }

    public void SetClass(string className)
    {
        this.className = className;

        Multiplayer_UIManager.Instance.menu_Panel.UpdateClass();
    }
    #endregion

    #region PrivateFunctions
    /// <summary>
    /// Handles creating or joining a room, with setup for Fusion.
    /// </summary>
    async void CreateOrJoinRoomOperation(string roomName)
    {
        Debug.Log($"[Fusion] Starting teacher session: {roomName}");

        if (_runner == null)
        {
            Debug.Log($"[Fusion] Starting teacher runner: {_runner == null}");
            _runner = Instantiate(runnerPrefab);
            _runner.ProvideInput = true;
        }

        // Ensure a scene manager is available
        if (!_runner.TryGetComponent<NetworkSceneManagerDefault>(out var sceneManager))
            sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var scene = SceneRef.FromIndex(2);

        if (!scene.IsValid)
        {
            Debug.LogError("[Fusion] Invalid scene index for multiplayer!");
            return;
        }

        string scenePath = SceneUtility.GetScenePathByBuildIndex(2);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        Debug.Log("Scene name: " + sceneName);
        string lobbyName = lobbyId + "_" + className + "_" + sceneName.Replace("_Multiplayer", "");

        Debug.Log("[Fusion] Joined lobby: " + lobbyName);

        var props = new Dictionary<string, SessionProperty>
        {
            ["pwHash"] = roomPassword        // keep as string
        };

        // Create Photon Realtime settings
        var photonSettings = appSettings.AppSettings;

        if (!string.IsNullOrEmpty(fusionRegion))
            photonSettings.FixedRegion = fusionRegion; // 👈 Change region here (e.g. "us", "eu", "in")

        // Start the shared game session
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            Scene = SceneRef.FromIndex(2),
            SceneManager = sceneManager,
            CustomLobbyName = lobbyName,
            PlayerCount = Constants.Network.studentsCount + 1,
            SessionProperties = props,
            // 👇 Cast AppSettings → FusionAppSettings
            CustomPhotonAppSettings = photonSettings
        });

        Debug.Log($"[Fusion] Teacher StartGame: {result.Ok}, Reason: {result.ShutdownReason}");
    }

    /// <summary>
    /// Registers and tracks a player when they join.
    /// </summary>
    void RegisteredPlayer(PlayerRef player, NetworkObject networkObj)
    {
        string playerName = $"Student {player.PlayerId}";
        PlayerNetworkData playerNetwork = null;

        if (networkObj.TryGetComponent<PlayerNetworkData>(out var ping))
        {
            playerNetwork = ping;
            playerName = playerNetwork.PlayerName.ToString();
        }

        players.Add(new PlayerData()
        {
            isLocalPlayer = _runner.LocalPlayer == player,
            playerId = player.PlayerId,
            playerName = playerNetwork != null ? playerNetwork.PlayerName.ToString() : "Student",
            playerMode = playerNetwork != null ? playerNetwork.PlayerMode : PlayerMode.Student,
            playerRef = player,
            data = playerNetwork
        });

        Multiplayer_UIManager.Instance.menu_Panel.UpdatePlayers(players);

        if (_runner.LocalPlayer != player)
        {
            Multiplayer_UIManager.Instance.menu_Panel.ShowLog("[" + playerNetwork.PlayerMode + "] " + playerName + " has joined the room.");
        }
    }

    /// <summary>
    /// Reset UI and local player when returning to lobby.
    /// </summary>
    void ResetUI()
    {
        isReconnecting = false;
        isResetData = false;

        SceneManager.LoadScene(lobbySceneName);
        Loading_Panel.instance.Close();
        Multiplayer_UIManager.Instance.menu_Panel.Close();
        Multiplayer_UIManager.Instance.menu_Panel.Open(0.5f);
        Multiplayer_UIManager.Instance.selectRole_Panel.Open(0.5f);
        roomStatus = RoomStatus.None;

        metaLocalPlayer.position = defaultPlayerPosition;
        metaLocalPlayer.eulerAngles = defaultPlayerRotation;
        metaLocalPlayerTeleport.localPosition = Vector3.zero;

        controllerButtonsManager.SetActive(false);

        players.Clear();
    }
    #endregion    
}

/// <summary>
/// Struct to track player info locally.
/// </summary>
[Serializable]
public struct PlayerData
{
    public bool isLocalPlayer;
    public int playerId;
    public string playerName;
    public PlayerMode playerMode;
    public PlayerRef playerRef;
    public PlayerNetworkData data;
}