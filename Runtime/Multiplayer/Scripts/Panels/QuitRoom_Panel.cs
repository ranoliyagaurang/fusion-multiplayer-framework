using System;
using DG.Tweening;
using UnityEngine;

namespace PTTI_Multiplayer
{
    public class QuitRoom_Panel : Panel
    {
        Action exitCallback;
        
        public void ShowPanel(Action action)
        {
            exitCallback = action;

            Open();
        }

        public override void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            base.Open(delay);
        }

        public override void Close(float delay = 0)
        {
            base.Close(delay);
        }

        public void NoClick()
        {
            Close();

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        public void YesClick()
        {
            Close();

            exitCallback?.Invoke();

            Multiplayer_SoundManager.Instance.PlayClick();

            FusionLobbyManager.Instance.roomStatus = RoomStatus.Disconnecting;

            Loading_Panel.instance.ShowLoading("Leaving room, please wait a moment.");

            if(FusionLobbyManager.Instance.IsNoTeacherPresentInRoom())
                NetworkSyncVariables.Instance.RPC_ExitSession();

            DOVirtual.DelayedCall(0.75f, () =>
            {
                FusionLobbyManager.Instance.ShutDownRunner();
            });
        }
    }
}