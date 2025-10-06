using System.Collections;
using Fusion;
using Fusion.Addons.Avatar.ReadyPlayerMe;
using PTTI_Multiplayer;
using UnityEngine;
using TMPro;

using static Fusion.Addons.Avatar.ReadyPlayerMe.RPMAvatarLoader;

public class PlayerNetworkData : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnPlayerModeChange))]
    public PlayerMode PlayerMode { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerNameChange))]
    public NetworkString<_32> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerAvatarURLChange))]
    public NetworkString<_128> PlayerAvatarURL { get; set; }

    [Networked] public int RttMs { get; set; }

    [Networked, OnChangedRender(nameof(OnPermissionChange))]
    public bool GrabPermission { get; set; }

    [Networked, OnChangedRender(nameof(OnMovementChange))]
    public bool LockMovement { get; set; }

    [Networked, OnChangedRender(nameof(OnMicChange))]
    public bool IsMicOn { get; set; }
    public bool HidePlayer { get; set; }

    public string localUserLayer = "InvisibleForLocalPlayer";
    public GameObject[] hideForLocalUserRenderers;
    public RPMAvatarLoader selectAvatarModel;

    [SerializeField] private TextMeshPro nameTag;
    [SerializeField] private Transform nameTagContainer;

    private Transform _centerEye;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        nameTagContainer.gameObject.SetActive(!Object.HasStateAuthority);

        OnPlayerNameChange();

        if (OVRManager.instance)
        {
            _centerEye = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().centerEyeAnchor;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasInputAuthority)
        {
            PlayerMode = FusionLobbyManager.Instance.playerMode;

            ShowHidePlayer(true);

            StartCoroutine(UpdatePing());
        }
        else
        {
            //Debug.LogError("PlayerNetworkData Spawned - PlayerMode: " + PlayerMode);

            if (PlayerMode == PlayerMode.Supervisor)
            {
                ShowHidePlayer(true);
            }
            else if (FusionLobbyManager.Instance.playerMode == PlayerMode.Student && PlayerMode == PlayerMode.Student)
            {
                StartCoroutine(HideStudentForOtherStudents());
            }
        }

        gameObject.name = "[" + PlayerMode + "] " + PlayerName;

        if (PlayerMode != PlayerMode.Supervisor)
            FusionLobbyManager.Instance.RegisterSpawnedPlayer(Object.InputAuthority, Object);

        if (!string.IsNullOrEmpty((string)PlayerAvatarURL))
        {
            //Debug.LogError("OnPlayerAvatarURLChange - " + PlayerAvatarURL);

            selectAvatarModel.isLocalAvatar = Object.HasInputAuthority;

            if (PlayerMode == PlayerMode.Student)
            {
                selectAvatarModel.avatarOptionalFeatures &= ~(OptionalFeatures.LipSynchronisation | OptionalFeatures.EyeMovementSimulation |
                                                          OptionalFeatures.EyeBlinking | OptionalFeatures.LipSyncWeightPonderation);
            }

            selectAvatarModel.ChangeAvatar((string)PlayerAvatarURL);
        }

        FusionLobbyManager.HideAvatar += OnHidePlayerChange;
    }

    void OnDestroy()
    {
        FusionLobbyManager.HideAvatar -= OnHidePlayerChange;
    }

    IEnumerator HideStudentForOtherStudents()
    {
        yield return new WaitUntil(() => selectAvatarModel.avatarInfo.avatarGameObject != null);

        if (LockMovement)
            ShowHidePlayer(true);
    }

    IEnumerator UpdatePing()
    {
        while (true)
        {
            RttMs = (int)(FusionLobbyManager.Instance._runner.GetPlayerRtt(Runner.LocalPlayer) * 1000f);

            yield return new WaitForSeconds(Constants.Network.pingDelay);
        }
    }

    public void OnPermissionChange()
    {
        //Debug.LogError("OnPermissionChange - " + GrabPermission);

        if(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher)
            Multiplayer_UIManager.Instance.menu_Panel.UpdateButtons();
    }

    public void OnMovementChange()
    {
        //Debug.LogError("OnMovementChange - " + LockMovement);

        if (FusionLobbyManager.Instance.playerMode == PlayerMode.Student && !Object.HasInputAuthority)
            ShowHidePlayer(LockMovement);

        if(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher)
            Multiplayer_UIManager.Instance.menu_Panel.UpdateButtons();
    }

    public void OnHidePlayerChange(bool hide)
    {
        //Debug.LogError("OnHidePlayerChange - " + LockMovement);

        HidePlayer = hide;

        ShowHidePlayer(HidePlayer);

        Multiplayer_UIManager.Instance.menu_Panel.UpdateButtons();
    }

    public void OnPlayerModeChange()
    {
        //Debug.LogError("OnPlayerModeChange - " + PlayerMode);

        if (PlayerMode == PlayerMode.Student && FusionLobbyManager.Instance.playerMode == PlayerMode.Student)
        {
            ShowHidePlayer(true);
        }
    }

    public void OnMicChange()
    {
        //Debug.LogError("OnMicChange - " + IsMicOn);

        Multiplayer_UIManager.Instance.menu_Panel.UpdateButtons();
    }

    public void OnPlayerNameChange()
    {
        //Debug.LogError("OnPlayerNameChange - " + PlayerName);

        gameObject.name = "[" + PlayerMode + "] " + PlayerName;

        nameTag.text = PlayerName.ToString();
    }

    public void OnPlayerAvatarURLChange()
    {
        //Debug.LogError("OnPlayerAvatarURLChange - " + PlayerAvatarURL);

        selectAvatarModel.isLocalAvatar = Object.HasInputAuthority;

        if (PlayerMode == PlayerMode.Student)
        {
            selectAvatarModel.avatarOptionalFeatures &= ~(OptionalFeatures.LipSynchronisation | OptionalFeatures.EyeMovementSimulation |
                                                      OptionalFeatures.EyeBlinking | OptionalFeatures.LipSyncWeightPonderation);
        }

        selectAvatarModel.ChangeAvatar((string)PlayerAvatarURL);
    }

    void ShowHidePlayer(bool isHide)
    {
        //Debug.Log("ShowHidePlayer - " + isHide);

        int layer = isHide ? LayerMask.NameToLayer(localUserLayer) : LayerMask.NameToLayer("Default");
        if (layer == -1)
        {
            Debug.LogError($"Local will be visible and may obstruct you vision. Please add a {localUserLayer} layer (it will be automatically removed on the camera culling mask)");
        }
        else
        {
            for (int i = 0; i < hideForLocalUserRenderers.Length; i++)
            {
                hideForLocalUserRenderers[i].layer = layer;
            }

            if (selectAvatarModel.avatarInfo.avatarGameObject != null)
            {
                SetLayerRecursively(selectAvatarModel.avatarInfo.avatarGameObject, layer);
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void Update()
    {
        if (nameTagContainer.gameObject.activeSelf == false)
        {
            return;
        }

        if (_centerEye != null)
        {
            nameTagContainer.transform.LookAt(_centerEye.position, Vector3.up);
        }
    }
}