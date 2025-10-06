using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace PTTI_Multiplayer
{
    public class JoinRoom_Panel : Panel
    {
        [SerializeField] RoomView roomPrefab;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] List<RoomView> roomViews = new();

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);

            for (int i = 0; i < roomViews.Count; i++)
            {
                roomViews[i].gameObject.SetActive(false);
            }
        }

        public void SessionListUpdate(List<SessionInfo> sessionList)
        {
            for (int i = 0; i < roomViews.Count; i++)
            {
                roomViews[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < sessionList.Count; i++)
            {
                if (i >= roomViews.Count)
                {
                    roomViews.Add(Instantiate(roomPrefab, scrollRect.content));
                }

                string roomPassword = (string)sessionList[i].Properties["pwHash"]; // example accessor

                roomViews[i].Bind(sessionList[i].Name, roomPassword);
            }
        }

        public void BackClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();

            if(FusionLobbyManager.Instance.playerMode == PlayerMode.Student)
                Multiplayer_UIManager.Instance.selectAvatar_Panel.Open(0.5f);
            else
                Multiplayer_UIManager.Instance.teacher_Panel.Open(0.5f);

            FusionLobbyManager.Instance.roomStatus = RoomStatus.None;

            FusionLobbyManager.Instance.ShutDownRunner();
        }
    }
}