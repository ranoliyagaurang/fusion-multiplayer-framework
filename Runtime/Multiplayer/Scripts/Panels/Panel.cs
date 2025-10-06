using DG.Tweening;
using System;
using UnityEngine;

namespace PTTI_Multiplayer
{
    [RequireComponent(typeof(Canvas))]
    public class Panel : MonoBehaviour
    {
        Canvas canvas;

        [Header("Panel Properties")]
        [SerializeField] Vector3 defaultScale;
        [SerializeField] GameObject ISDK_RayCanvasInteraction;
        [SerializeField] bool isOpen = false;

        public bool IsOpen
        {
            get { return isOpen; }
        }

        void Awake()
        {
            canvas = GetComponent<Canvas>();

            if (defaultScale == Vector3.zero)
                defaultScale = transform.localScale;

            //transform.localScale = Vector3.zero;

            ISDK_RayCanvasInteraction.SetActive(false);

            canvas.enabled = false;
        }

        public virtual void Open(float delay = 0, string msg = "", AudioClip audioClip = null)
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;

            transform.localScale = Vector3.zero;
            transform.DOScale(defaultScale, 0.2f).SetDelay(delay).OnComplete(OnCompleteTween).OnStart(OnStartOpen);

            ISDK_RayCanvasInteraction.SetActive(true);
        }

        public virtual void ShowMessage(float delay = 0, string msg = "", bool playSound = true, bool enableBt = true)
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;

            transform.localScale = Vector3.zero;
            transform.DOScale(defaultScale, 0.2f).SetDelay(delay).OnComplete(OnCompleteTween).OnStart(OnStartOpen);

            ISDK_RayCanvasInteraction.SetActive(true);
        }

        public virtual void Open(Action action, Transform panel, float delay = 0)
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;

            transform.localScale = Vector3.zero;
            transform.DOScale(defaultScale, 0.2f).SetDelay(delay).OnComplete(OnCompleteTween).OnStart(OnStartOpen);

            ISDK_RayCanvasInteraction.SetActive(true);
        }

        void OnStartOpen()
        {
            canvas.enabled = true;
            transform.localScale = Vector3.zero;
        }

        void OnCompleteTween()
        {
            Vector3 scale = defaultScale * 1.1f;
            transform.DOScale(scale, 0.2f).OnComplete(OnOpenComplete);
        }

        public virtual void OnOpenComplete()
        {
            transform.DOScale(defaultScale, 0.2f).Complete();
        }

        public virtual void Close(float delay = 0)
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            
            transform.DOScale(Vector3.zero, 0.35f).SetDelay(delay).OnComplete(OnCloseComplete);

            ISDK_RayCanvasInteraction.SetActive(false);
        }

        public virtual void OnCloseComplete()
        {
            canvas.enabled = false;
        }
    }
}