using DG.Tweening;
using HVAC;
using UnityEngine;

public class AndroidPayloadManager : MonoBehaviour
{
    public static AndroidPayloadManager Instance;

    public CustomPayloadData payloadData;
    public bool isCreateRoom;

    void Awake()
    {
        Instance = this;

        FetchPlayerDataFromLastLogin();
    }

    void Start()
    {
#if UNITY_EDITOR
        LoadRolePanel();
#endif
    }

    public void FetchPlayerDataFromLastLogin()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

            string payloadJson = intent.Call<string>("getStringExtra", "payload_json");

            if (!string.IsNullOrEmpty(payloadJson))
            {
                payloadData = JsonUtility.FromJson<CustomPayloadData>(payloadJson);

                CreateOrJoinRoom();

                Debug.Log("payloadData: " + payloadJson);
            }
            else
            {
                Debug.LogWarning("No login JSON found.");

                LoadRolePanel();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to receive login JSON: " + e.Message);

            LoadRolePanel();
        }
#endif
    }

    public string GetPlayerDataOfCurrentLogin()
    {
        CustomPayloadData payloadData = new()
        {
            playerName = FusionLobbyManager.Instance.playerName,
            className = FusionLobbyManager.Instance.className,
            regionName = FusionLobbyManager.Instance.connectedFusionRegion,
            playerMode = FusionLobbyManager.Instance.playerMode,
            avatarURL = FusionLobbyManager.Instance.avatarURL,
            isCreateRoom = isCreateRoom
        };

        return JsonUtility.ToJson(payloadData);
    }

    public void CreateOrJoinRoom()
    {
        FusionLobbyManager.Instance.playerName = payloadData.playerName;
        FusionLobbyManager.Instance.playerMode = payloadData.playerMode;
        FusionLobbyManager.Instance.avatarURL = payloadData.avatarURL;
        FusionLobbyManager.Instance.className = payloadData.className;
        FusionLobbyManager.Instance.fusionRegion = payloadData.regionName;

        DOVirtual.DelayedCall(0.1f, () =>
        {
            if (payloadData.playerMode == PlayerMode.Teacher)
            {
                if (payloadData.isCreateRoom)
                {
                    PTTI_Multiplayer.Loading_Panel.instance.ShowLoading("Creating room, please wait a moment.");
                    FusionLobbyManager.Instance.roomStatus = RoomStatus.Creating;
                    FusionLobbyManager.Instance.CreateOrJoinRoom(Random.Range(000000, 999999).ToString());
                }
                else
                {
                    FusionLobbyManager.Instance.InitRunner();
                    Multiplayer_UIManager.Instance.joinRoom_Panel.Open(0.5f);
                }
            }
            else
            {
                FusionLobbyManager.Instance.InitRunner();
                Multiplayer_UIManager.Instance.joinRoom_Panel.Open(0.5f);
            }
        });
    }

    public void LoadRolePanel()
    {
        //Debug.Log("LoadRolePanel");

        DOVirtual.DelayedCall(1f, () =>
        {
            Multiplayer_UIManager.Instance.selectRole_Panel.Open(0.5f);
        });
    }
}