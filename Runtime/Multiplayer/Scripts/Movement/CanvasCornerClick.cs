using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CanvasCornerClick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    [Header("Handler")]
    [SerializeField] CanvasCornerResizer canvasCornerResizer;

    [Header("Mode")]
    public bool isMoveHandle = false; // true = move handle (side), false = resize handle (corner)

    Image iconImage;
    Transform pointerRef;
    Tween tween;
    bool isPressed = false;

    void OnEnable()
    {
        ControllerButtonsManager.LeftSelected += OnSelected;
        ControllerButtonsManager.RightSelected += OnSelected;
    }

    void OnDisable()
    {
        ControllerButtonsManager.LeftSelected -= OnSelected;
        ControllerButtonsManager.RightSelected -= OnSelected;
    }

    void OnSelected(Transform pointer)
    {
        pointerRef = pointer;
    }

    void Start()
    {
        iconImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerRef == null) return;

        canvasCornerResizer.isMoveHandle = isMoveHandle;
        canvasCornerResizer.OnPointerDown();

        tween?.Kill();

        tween = iconImage.DOColor(Color.blue, 0.5f);

        isPressed = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (pointerRef == null) return;

        canvasCornerResizer.OnDrag();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPressed) return;

        tween?.Kill();

        tween = iconImage.DOColor(Color.yellow, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPressed) return;

        tween?.Kill();

        tween = iconImage.DOColor(Color.white, 0.5f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        canvasCornerResizer.OnPointerUp();

        tween?.Kill();

        tween = iconImage.DOColor(Color.white, 0.5f);
    }
}