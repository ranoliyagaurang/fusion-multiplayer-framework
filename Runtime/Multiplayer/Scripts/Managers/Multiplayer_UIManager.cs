using System.Collections.Generic;
using Oculus.Interaction.Locomotion;
using PTTI_Multiplayer;
using UnityEngine;
using UnityEngine.XR;

public class Multiplayer_UIManager : MonoBehaviour
{
    public static Multiplayer_UIManager Instance;

    [Header("Panels")]
    public SelectRole_Panel selectRole_Panel;
    public SelectClass_Panel selectClass_Panel;
    public SelectAvatar_Panel selectAvatar_Panel;
    public JoinRoom_Panel joinRoom_Panel;
    public Teacher_Panel teacher_Panel;
    public EnterPassword_Panel enterPassword_Panel;
    public Message_Panel message_Panel;
    public Menu_Panel menu_Panel;
    public QuitRoom_Panel quitRoom_Panel;
    public Reconnect_Panel reconnect_Panel;
    public EnterRoomPassword_Panel enterRoomPassword_Panel;

    [Header("Events")]
    public LocomotionEventsConnection locomotionEventsConnection;

    public bool IsInternetAvailable()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            message_Panel.ShowPanel(0f, "No internet connection available. Please check your network settings.");
        }
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    void Awake()
    {
        Instance = this;

        Application.targetFrameRate = 72;

        XRSettings.useOcclusionMesh = false;
    }

    void Start()
    {
        locomotionEventsConnection.WhenLocomotionPerformed += OnTeleportPerformed;
    }

    public void OpenPanelAfterDisconnect()
    {
        if (FusionLobbyManager.Instance.playerMode == PlayerMode.Student)
        {
            joinRoom_Panel.Open(0.5f);
        }
        else
        {
            selectRole_Panel.Open(0.5f);
        }
    }

    public void ResetView()
    {
        List<XRInputSubsystem> subsystems = new();
        SubsystemManager.GetSubsystems(subsystems); // New API, no obsolete warning

        foreach (var subsystem in subsystems)
        {
            subsystem.TryRecenter();
        }
    }

    void OnTeleportPerformed(LocomotionEvent locomotion)
    {
        //Debug.Log("OnTeleportPerformed");

        FusionLobbyManager.Instance.FetchLocalPlayerWhenTeleported();
    }
}