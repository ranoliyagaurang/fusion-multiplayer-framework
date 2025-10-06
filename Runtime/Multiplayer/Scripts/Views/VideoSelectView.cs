using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoSelectView : MonoBehaviour
{
    [SerializeField] RawImage iconImg;
    [SerializeField] TextMeshProUGUI titleTxt;

    Action<int> callback;

    public void Off()
    {
        gameObject.SetActive(false);
    }

    public void Bind(string title, Texture2D icon, Action<int> callback)
    {
        this.callback = callback;

        titleTxt.text = title;
        iconImg.texture = icon;

        gameObject.SetActive(true);
    }

    public void OnClick()
    {
        callback?.Invoke(transform.GetSiblingIndex());
    }
}