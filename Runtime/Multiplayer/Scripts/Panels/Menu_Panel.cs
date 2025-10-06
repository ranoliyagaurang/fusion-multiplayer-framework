using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion.Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PTTI_Multiplayer
{
    public class Menu_Panel : Panel
    {
        [SerializeField] RectTransform canvasRectTransform;
        [SerializeField] RectTransform bgRectTransform;
        [SerializeField] RectTransform scrollRectTransform;
        public int minChildCount = 4;
        public int maxChildCount = 10;
        public float minHeight = 745f;
        public float maxHeight = 2245f;
        public float childHeight = 200f;

        [Header("RoomData")]
        [SerializeField] TextMeshProUGUI studentCountText;
        [SerializeField] TextMeshProUGUI roomCode;
        [SerializeField] TextMeshProUGUI pingText;
        [SerializeField] TextMeshProUGUI versionText;
        [SerializeField] TextMeshProUGUI classText;
        [SerializeField] TextMeshProUGUI grabPermissionText;
        [SerializeField] TextMeshProUGUI lockMovementText;
        [SerializeField] TextMeshProUGUI isMuteText;
        [SerializeField] TextMeshProUGUI appRegionText;
        [SerializeField] Toggle settingsToggle;
        [SerializeField] Toggle studentListToggle;
        [SerializeField] Toggle studentLogsToggle;
        [SerializeField] Toggle debugsToggle;
        [SerializeField] GameObject settingsObj;
        [SerializeField] GameObject studentListObj;
        [SerializeField] GameObject studentLogsObj;
        [SerializeField] Toggle micToggle;
        [SerializeField] GameObject passthroughBt;
        [SerializeField] GameObject disablePassthroughBt;
        [SerializeField] GameObject menuBt;
        [SerializeField] CanvasCornerResizer canvasScalingMovement;
        [SerializeField] GameObject flipCanvas;

        [Header("StudentList")]
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] PlayerStatusView playerStatusViewPrefab;
        [SerializeField] List<PlayerStatusView> playerStatusViews = new();
        [SerializeField] TextMeshProUGUI micBtText;
        [SerializeField] TextMeshProUGUI lockBtText;
        [SerializeField] TextMeshProUGUI hideBtText;
        [SerializeField] TextMeshProUGUI grabBtText;
        [SerializeField] Button lockBt;
        [SerializeField] Button micBt;
        [SerializeField] Button hideBt;
        [SerializeField] Button resetBt;
        [SerializeField] Button grabBt;
        public PlayerData localPlayer;

        [Header("StudentLogs")]
        [SerializeField] ScrollRect logsScrollRect;
        [SerializeField] LogsView logsViewPrefab;
        [SerializeField] List<LogsView> logViews = new();
        int index;

        [Header("Graphy")]
        [SerializeField] GameObject graphyCanvas;

        [Header("DebugPanel")]
        [SerializeField] Canvas debugConsole;
        [SerializeField] GameObject debugConsoleISDK_RayCanvasInteraction;

        [Header("Photon Settings")]
        // Photon settings (versioning)
        [SerializeField] PhotonAppSettings appSettings;

        Coroutine pingCoroutine;
        bool isMicOnAll = false;
        bool isHideAll = false;
        bool isLockAll = false;
        bool isGrabAll = false;

        public PlayerNetworkData localPlayerData = null;

        void Start()
        {
            if (appSettings != null && appSettings.AppSettings.FixedRegion != "asia")
            {
                debugsToggle.gameObject.SetActive(false);
            }

            flipCanvas.SetActive(false);
        }

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            // if (IsOpen)
            //     return;

            base.Open(delay);

            micBtText.text = isMicOnAll ? "Mute All" : "Unmute All";
            hideBtText.text = isHideAll ? "Hide All" : "Show All";
            lockBtText.text = isLockAll ? "Unlock All" : "Lock All";
            grabBtText.text = isGrabAll ? "UnGrab All" : "Grab All";

            if (FusionLobbyManager.Instance.roomStatus != RoomStatus.InRoom)
            {
                micToggle.gameObject.SetActive(false);

                passthroughBt.SetActive(false);
                disablePassthroughBt.SetActive(false);

                ActiveMenuButtons(false);

                pingText.text = ": 0 ms";
                roomCode.text = ": 000000";
                classText.text = ": -";
                grabPermissionText.text = ": -";
                lockMovementText.text = ": -";
                isMuteText.text = ": -";

                menuBt.SetActive(false);
            }
            else
            {
                micToggle.gameObject.SetActive(true);

                menuBt.SetActive(true);

                if (FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher)
                {
                    Invoke(nameof(UpdatePassthroughButtons), 1);
                }
                else
                {
                    passthroughBt.SetActive(false);
                    disablePassthroughBt.SetActive(false);
                }

                micToggle.isOn = FusionLobbyManager.Instance.recorder.RecordingEnabled;

                string fullName = FusionLobbyManager.Instance.className; // "Class_A"
                string[] parts = fullName.Split('_');                   // ["Class", "A"]
                string shortName = parts.Length > 1 ? parts[1] : fullName;
                classText.text = ": " + shortName;

                ActiveMenuButtons(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher);

                if (localPlayerData == null)
                    localPlayerData = FusionLobbyManager.Instance.localPlayerData;

                if (pingCoroutine != null)
                    StopCoroutine(pingCoroutine);

                pingCoroutine = StartCoroutine(ShowPing());

                Invoke(nameof(UpdateStatus), 1);
            }

            versionText.text = ": " + Application.version;

            canvasScalingMovement.gameObject.SetActive(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher);
            flipCanvas.SetActive(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher);

            appRegionText.text = ": " + FusionLobbyManager.Instance.connectedFusionRegion;

            if (FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher)
            {
                canvasScalingMovement.GetRoomBounds();
            }
        }

        public void UpdateRegion()
        {
            appRegionText.text = ": " + FusionLobbyManager.Instance.connectedFusionRegion;
        }

        public void UpdateClass()
        {
            if (!string.IsNullOrEmpty(FusionLobbyManager.Instance.className))
            {
                string fullName = FusionLobbyManager.Instance.className; // "Class_A"
                string[] parts = fullName.Split('_');                   // ["Class", "A"]
                string shortName = parts.Length > 1 ? parts[1] : fullName;
                classText.text = ": " + shortName;
            }
            else
            {
                classText.text = ": -";
            }
        }

        public void UpdateMicToggle()
        {
            micToggle.isOn = FusionLobbyManager.Instance.recorder.RecordingEnabled;
        }

        void UpdatePassthroughButtons()
        {
            passthroughBt.SetActive(!NetworkSyncVariables.Instance.IsPassthroughEnabled);
            disablePassthroughBt.SetActive(NetworkSyncVariables.Instance.IsPassthroughEnabled);
        }

        public override void Close(float delay = 0)
        {
            if (!IsOpen)
                return;

            base.Close(delay);

            debugConsole.enabled = false;
            debugConsoleISDK_RayCanvasInteraction.SetActive(false);

            if (pingCoroutine != null)
                StopCoroutine(pingCoroutine);

            graphyCanvas.SetActive(false);

            for (int i = 0; i < playerStatusViews.Count; i++)
            {
                playerStatusViews[i].Off();
            }
        }

        void ActiveMenuButtons(bool active)
        {
            if (active)
            {
                studentListToggle.isOn = true;

                UpdateButtons();

                studentListToggle.gameObject.SetActive(true);
                studentLogsToggle.gameObject.SetActive(true);

                studentListObj.SetActive(studentListToggle.isOn);
                studentLogsObj.SetActive(studentLogsToggle.isOn);
            }
            else
            {
                settingsToggle.isOn = true;

                studentListToggle.gameObject.SetActive(false);
                studentLogsToggle.gameObject.SetActive(false);

                studentListObj.SetActive(false);
                studentLogsObj.SetActive(false);
            }

            settingsObj.SetActive(settingsToggle.isOn);

            debugConsole.enabled = debugsToggle.isOn;
            debugConsoleISDK_RayCanvasInteraction.SetActive(debugsToggle.isOn);

            graphyCanvas.SetActive(settingsToggle.isOn);
        }

        public void ToggleClick()
        {
            if (FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher)
            {
                studentListObj.SetActive(studentListToggle.isOn);
                studentLogsObj.SetActive(studentLogsToggle.isOn);
            }

            settingsObj.SetActive(settingsToggle.isOn);
            graphyCanvas.SetActive(settingsToggle.isOn);

            debugConsole.enabled = debugsToggle.isOn;
            debugConsoleISDK_RayCanvasInteraction.SetActive(debugsToggle.isOn);

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void MicToggle()
        {
            micToggle.isOn = !micToggle.isOn;

            FusionLobbyManager.Instance.Mic(micToggle.isOn);

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void MainMenuClick()
        {
            Multiplayer_UIManager.Instance.quitRoom_Panel.Open();

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void CloseClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void UpdatePassthroughToggle(bool active)
        {
            passthroughBt.SetActive(!active);
            disablePassthroughBt.SetActive(active);
        }

        public void PassthroughClick()
        {
            NetworkSyncVariables.Instance.SetPassthrough();

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        #region Student List
        public void UpdatePlayers(List<PlayerData> players)
        {
            for (int i = 0; i < playerStatusViews.Count; i++)
            {
                playerStatusViews[i].Off();
            }

            if (localPlayer.data == null)
            {
                localPlayer = players.Find(x => x.isLocalPlayer);

                players.Remove(localPlayer);
            }

            for (int i = 0; i < players.Count; i++)
            {
                if (i >= playerStatusViews.Count)
                {
                    playerStatusViews.Add(Instantiate(playerStatusViewPrefab, scrollRect.content));
                }

                playerStatusViews[i].Bind(players[i]);
            }

            studentCountText.text = ": " + $"{players.Count:00}/" + Constants.Network.studentsCount;

            UpdateButtons();

            //UpdateRectSize();
        }

        public void ShowRoomCode(string code)
        {
            roomCode.text = ": " + code;
        }

        IEnumerator ShowPing()
        {
            while (true)
            {
                if (localPlayerData != null)
                    pingText.text = ": " + $"{localPlayerData.RttMs} ms";

                for (int i = 0; i < playerStatusViews.Count; i++)
                {
                    playerStatusViews[i].UpdatePingAndMic();
                }
                //Debug.Log("Ping: " + (localPlayerData != null ? localPlayerData.RttMs.ToString() : "0"));

                yield return new WaitForSeconds(Constants.Network.pingDelay);
            }
        }

        public void MicOnAllClick()
        {
            isMicOnAll = !isMicOnAll;

            micBtText.text = isMicOnAll ? "Mute All" : "Unmute All";

            NetworkSyncVariables.Instance.MicOnAllPlayers(isMicOnAll);

            for (int i = 0; i < playerStatusViews.Count; i++)
            {
                playerStatusViews[i].UpdateMicToggle(isMicOnAll);
            }

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void GrabAllClick()
        {
            isGrabAll = !isGrabAll;

            grabBtText.text = isGrabAll ? "UnGrab All" : "Grab All";

            NetworkSyncVariables.Instance.SendPermissionToAllPlayers(isGrabAll);

            for (int i = 0; i < playerStatusViews.Count; i++)
            {
                playerStatusViews[i].UpdateGrabToggle(isGrabAll);
            }

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void HideAllClick()
        {
            isHideAll = !isHideAll;

            hideBtText.text = isHideAll ? "Show All" : "Hide All";

            FusionLobbyManager.Instance.HidePlayerAvatar(isHideAll);

            for (int i = 0; i < playerStatusViews.Count; i++)
            {
                playerStatusViews[i].UpdateHideToggle(isHideAll);
            }

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void ResetAllClick()
        {
            NetworkSyncVariables.Instance.ResetAllPlayers();

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void LockAllClick()
        {
            isLockAll = !isLockAll;

            lockBtText.text = isLockAll ? "Unlock All" : "Lock All";

            NetworkSyncVariables.Instance.LockAllPlayers(isLockAll);

            for (int i = 0; i < playerStatusViews.Count; i++)
            {
                playerStatusViews[i].UpdateLockToggle(isLockAll);
            }

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void UpdateButtons()
        {
            List<PlayerStatusView> views = playerStatusViews.FindAll(x => x.IsStudent());
            int countPlayers = views.Count;

            if (countPlayers == 0)
            {
                lockBt.interactable = false;
                micBt.interactable = false;
                resetBt.interactable = false;
                hideBt.interactable = false;
                grabBt.interactable = false;
                return;
            }

            lockBt.interactable = true;
            micBt.interactable = true;
            resetBt.interactable = true;
            hideBt.interactable = true;
            grabBt.interactable = true;

            bool isLockAll = views.All(x => x.IsLockMovement());

            this.isLockAll = isLockAll;

            lockBtText.text = this.isLockAll ? "Unlock All" : "Lock All";

            bool isMicOnAll = views.All(x => x.IsMicOn());

            this.isMicOnAll = isMicOnAll;

            micBtText.text = this.isMicOnAll ? "Mute All" : "Unmute All";

            bool isHideAll = views.All(x => x.IsHidePlayer());

            this.isHideAll = isHideAll;

            hideBtText.text = this.isHideAll ? "Show All" : "Hide All";

            bool isGrabAll = views.All(x => x.HasGrabPermission());

            this.isGrabAll = isGrabAll;

            grabBtText.text = isGrabAll ? "UnGrab All" : "Grab All";

            UpdatePlayersToggle();
        }

        public void UpdateStatus()
        {
            if (localPlayerData == null) return;
            grabPermissionText.text = localPlayerData.GrabPermission ? "<color=green>: YES</color>" : "<color=red>: NO</color>";
            lockMovementText.text = localPlayerData.LockMovement ? "<color=red>: YES</color>" : "<color=green>: NO</color>";
            isMuteText.text = localPlayerData.IsMicOn ? "<color=green>: NO</color>" : "<color=red>: YES</color>";
        }

        public void UpdatePlayersToggle()
        {
            for (int i = 0; i < playerStatusViews.Count; i++)
            {
                playerStatusViews[i].UpdateAllToggles();
            }
        }
        #endregion

        #region Student Logs
        public void ShowLog(string msg)
        {
            if (index >= logViews.Count)
            {
                logViews.Add(Instantiate(logsViewPrefab, logsScrollRect.content));
            }

            logViews[index].Bind(msg);

            index++;
        }

        public void ClearAllClick()
        {
            for (int i = 0; i < logViews.Count; i++)
            {
                logViews[i].Off();
            }

            index = 0;

            Multiplayer_SoundManager.Instance.PlayClick();
        }
        #endregion

        #region Resize BG
        void UpdateRectSize()
        {
            if (scrollRect == null) return;

            int childCount = scrollRect.content.childCount;

            float newHeight;

            if (childCount <= minChildCount)
            {
                newHeight = minHeight;
            }
            else if (childCount == maxChildCount)
            {
                newHeight = maxHeight;
            }
            else if (childCount > maxChildCount)
            {
                newHeight = maxHeight + 50;
            }
            else
            {
                // interpolate between minHeight and maxHeight
                float t = GetTargetHeight(childCount, childHeight);
                Debug.Log($"t: {t}");
                newHeight = Mathf.Clamp(t, minHeight, maxHeight);
            }

            // Apply height (preserve width)
            var size = scrollRectTransform.sizeDelta;
            size.y = newHeight;
            scrollRectTransform.sizeDelta = size;

            canvasRectTransform.sizeDelta = new Vector2(canvasRectTransform.sizeDelta.x, newHeight + 255);
            bgRectTransform.sizeDelta = new Vector2(bgRectTransform.sizeDelta.x, newHeight + 255);
        }

        float GetTargetHeight(int childCount, float childHeight)
        {
            float padding = 10 + 10; // top + bottom
            float spacing = 20 * Mathf.Max(0, childCount - 1);
            return padding + (childHeight * childCount) + spacing;
        }
        #endregion
    }
}