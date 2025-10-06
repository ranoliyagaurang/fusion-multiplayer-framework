using UnityEngine;
using UnityEngine.UI;

public class SlideView : MonoBehaviour
{
    [SerializeField] Image slideImg;
    
    public void Bind(Sprite sprite, Vector2 size)
    {
        slideImg.sprite = sprite;

        slideImg.rectTransform.sizeDelta = size;
        
        gameObject.SetActive(true);
    }

    public void Open()
    {

    }

    public void Close()
    {

    }

    public void Off()
    {
        gameObject.SetActive(false);
    }
}