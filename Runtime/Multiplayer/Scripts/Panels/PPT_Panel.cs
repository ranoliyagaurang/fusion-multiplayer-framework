using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using Fusion;
using System.Collections;
using System;

namespace PTTI_Multiplayer
{
    public class PPT_Panel : NetworkBehaviour
    {
        [Header("Panel Refs")]
        [SerializeField] GameObject pptMenuPanel; // Panel with video controls
        [SerializeField] GameObject pptPlayerPanel; // Panel with VideoPlayer
        [SerializeField] PPTList pptListSO; // ScriptableObject with VideoClip list
        [SerializeField] PPTSelectView pptSelectViewPrefab; // Prefab for video selection buttons
        [SerializeField] List<PPTSelectView> pptSelectViews = new(); // Instantiated buttons
        [SerializeField] ScrollRect pptSelectScrollRect; // ScrollRect containing buttons

        [Header("NetworkProperties")]
        [Networked, OnChangedRender(nameof(OnSlideIndexChanged))]
        public int CurrentIndex { get; set; }

        [Networked, OnChangedRender(nameof(OnPPTChanged))]
        public int CurrentPPT { get; set; }

        [Header("SlideData")]
        [SerializeField] Slide_List slideList;

        [Header("UI")]
        [SerializeField] TextMeshProUGUI titleTxt;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] SlideView slideViewPrefab;
        [SerializeField] List<SlideView> slideViews = new();
        [SerializeField] float scrollDuration = 0.5f; // Smooth scroll time

        int totalSlides;
        private Coroutine waitAuthorityRoutine;
        int tempPPTIndex;

        void Awake()
        {
            pptMenuPanel.SetActive(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher);
            pptPlayerPanel.SetActive(FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher);
        }

        public override void Spawned()
        {
            base.Spawned();

            if (Object.HasStateAuthority)
            {
                pptMenuPanel.SetActive(true);
                pptPlayerPanel.SetActive(false);

                ListPPT();
            }
            else
            {
                pptMenuPanel.SetActive(false);
                pptPlayerPanel.SetActive(true);
            }
            
            Open();
        }

        public void Open()
        {
            if (CurrentPPT == -1)
                slideList = pptListSO.pPTEntries[0].slide_List;
            else
                slideList = pptListSO.pPTEntries[CurrentPPT].slide_List;

            for (int i = 0; i < slideViews.Count; i++)
            {
                slideViews[i].Off();
            }

            for (int i = 0; i < slideList.slides.Count; i++)
            {
                if (slideViews.Count <= i)
                    slideViews.Add(Instantiate(slideViewPrefab, scrollRect.content));

                slideViews[i].Bind(slideList.slides[i].sprite, slideList.imageSize);
            }

            // Count total slides under Content
            totalSlides = slideList.slides.Count;

            for (int i = 0; i < totalSlides; i++)
            {
                slideViews[i].Close();
            }

            slideViews[CurrentIndex].Open();

            DOVirtual.DelayedCall(0.1f, () =>
            {
                ScrollToIndex(CurrentIndex, true);
            });

            titleTxt.text = "Slide - " + (CurrentIndex + 1) + "/" + totalSlides;
        }

        public void NextClick()
        {
            if (Object.HasStateAuthority)
            {
                NextSlide();
            }
            else
            {
                // start polling until we get authority
                if (waitAuthorityRoutine != null)
                    StopCoroutine(waitAuthorityRoutine);

                waitAuthorityRoutine = StartCoroutine(WaitForAuthority(NextSlide));
            }

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        void NextSlide()
        {
            if (CurrentIndex < totalSlides - 1)
            {
                CurrentIndex++;
                ScrollToIndex(CurrentIndex);
            }
        }

        public void PreviousClick()
        {
            if (Object.HasStateAuthority)
            {
                PrevSlide();
            }
            else
            {
                // start polling until we get authority
                if (waitAuthorityRoutine != null)
                    StopCoroutine(waitAuthorityRoutine);

                waitAuthorityRoutine = StartCoroutine(WaitForAuthority(PrevSlide));
            }

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        void PrevSlide()
        {
            if (CurrentIndex > 0)
            {
                CurrentIndex--;
                ScrollToIndex(CurrentIndex);
            }
        }

        void ScrollToIndex(int index, bool instant = false)
        {
            if (totalSlides <= 1) return;

            // Correct formula: leftmost = 0, rightmost = 1
            float targetPos = index / (float)(totalSlides - 1);

            // If already at target, just return (no extra tween)
            if (Mathf.Approximately(scrollRect.horizontalNormalizedPosition, targetPos))
                return;

            DOTween.Kill(scrollRect);

            if (instant)
            {
                scrollRect.horizontalNormalizedPosition = targetPos;
            }
            else
            {
                for (int i = 0; i < slideList.slides.Count; i++)
                {
                    slideViews[i].Close();
                }

                DOTween.To(
                    () => scrollRect.horizontalNormalizedPosition,
                    x => scrollRect.horizontalNormalizedPosition = x,
                    targetPos,
                    scrollDuration
                ).SetEase(Ease.OutCubic)
                 .SetTarget(scrollRect).OnComplete(() =>
                 {
                     slideViews[index].Open();
                 });
            }

            titleTxt.text = "Slide - " + (CurrentIndex + 1) + "/" + totalSlides;
        }

        void OnSlideIndexChanged()
        {
            ScrollToIndex(CurrentIndex);
        }

        void OnPPTChanged()
        {
            slideList = pptListSO.pPTEntries[CurrentPPT].slide_List;

            Open();
        }

        public void MenuClick()
        {
            pptMenuPanel.SetActive(true);
            pptPlayerPanel.SetActive(false);

            ListPPT();

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        void ListPPT()
        {
            for (int i = 0; i < pptSelectViews.Count; i++)
            {
                pptSelectViews[i].Off();
            }

            for (int i = 0; i < pptListSO.pPTEntries.Length; i++)
            {
                if (pptSelectViews.Count <= i)
                {
                    pptSelectViews.Add(Instantiate(pptSelectViewPrefab, pptSelectScrollRect.content));
                }

                pptSelectViews[i].Bind(pptListSO.pPTEntries[i].pptName, pptListSO.pPTEntries[i].thumbnail, SelectPPTByTeacher);
            }
        }

        void SelectPPTByTeacher(int index)
        {
            if (Object.HasStateAuthority)
            {
                tempPPTIndex = index;
                SelectPPT();
            }
            else
            {
                tempPPTIndex = index;

                // start polling until we get authority
                if (waitAuthorityRoutine != null)
                    StopCoroutine(waitAuthorityRoutine);

                waitAuthorityRoutine = StartCoroutine(WaitForAuthority(SelectPPT));
            }

            Multiplayer_SoundManager.Instance.PlayClick();
        }

        void SelectPPT()
        {
            pptMenuPanel.SetActive(false);
            pptPlayerPanel.SetActive(true);

            CurrentPPT = tempPPTIndex;

            CurrentIndex = 0;

            titleTxt.text = "Slide - " + (CurrentIndex + 1) + "/" + totalSlides;

            RPC_HideMenu();
        }

        private IEnumerator WaitForAuthority(Action action)
        {
            Object.RequestStateAuthority();

            yield return new WaitUntil(() => Object.HasStateAuthority);

            if (Object.HasStateAuthority)
            {
                // ✅ continue original click flow once we got authority

                action?.Invoke();
            }

            waitAuthorityRoutine = null;
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_HideMenu(RpcInfo info = default)
        {
            if (info.IsInvokeLocal)
            {
                return;
            }

            //Debug.LogError("RPC_MutePlayer - " + playerIds.Length + " : " + mute);

            pptMenuPanel.SetActive(false);
            pptPlayerPanel.SetActive(true);
        }

        public void BackMenuClick()
        {
            pptMenuPanel.SetActive(false);
            pptPlayerPanel.SetActive(true);

            Multiplayer_SoundManager.Instance.PlayClick();
        }
    }
}