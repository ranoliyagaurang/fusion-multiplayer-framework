using System;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class ControllerButtonsManager : MonoBehaviour
{
    public delegate void OnLeftSelected(Transform pointer);
    public static event OnLeftSelected LeftSelected;

    public delegate void OnRightSelected(Transform pointer);
    public static event OnRightSelected RightSelected;

    public delegate void OnDistanceHandGrabHover();
    public static event OnDistanceHandGrabHover DistanceHandGrabHover;

    public delegate void OnDistanceHandGrabSelected(Transform pointer);
    public static event OnDistanceHandGrabSelected DistanceHandGrabSelected;

    public static event Action<Vector2> OnLeftStickChanged;
    public static event Action<Vector2> OnRightStickChanged;

    public static ControllerButtonsManager Instance;

    public ControllerSelector leftController;
    public ControllerSelector rightController;

    public DistanceHandGrabInteractor leftInteractor;
    public DistanceHandGrabInteractor rightInteractor;

    [SerializeField] Transform leftCursor;
    [SerializeField] Transform rightCursor;

    private Vector2 lastLeft;
    private Vector2 lastRight;

    void Update()
    {
        Vector2 left = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        Vector2 right = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

        if (left != lastLeft)
        {
            OnLeftStickChanged?.Invoke(left);
            lastLeft = left;
        }

        if (right != lastRight)
        {
            OnRightStickChanged?.Invoke(right);
            lastRight = right;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Multiplayer_UIManager.Instance.menu_Panel.Open();
    }

    void OnEnable()
    {
        leftController.WhenSelected += Left_Selected;
        leftController.WhenUnselected += Left_UnSelected;

#if !UNITY_EDITOR
        rightController.WhenSelected += Right_Selected;
        rightController.WhenUnselected += Right_UnSelected;
#endif
        // Hover
        rightInteractor.WhenInteractableSet.Action += OnHoverEnter;
        rightInteractor.WhenInteractableUnset.Action += OnHoverExit;

        // Grab / release
        rightInteractor.WhenInteractableSelected.Action += OnSelect;
        rightInteractor.WhenInteractableUnselected.Action += OnUnselect;
    }

    void OnDisable()
    {
        leftController.WhenSelected -= Left_Selected;
        leftController.WhenUnselected -= Left_UnSelected;

#if !UNITY_EDITOR
        rightController.WhenSelected -= Right_Selected;
        rightController.WhenUnselected -= Right_UnSelected;
#endif

        // Hover
        rightInteractor.WhenInteractableSet.Action -= OnHoverEnter;
        rightInteractor.WhenInteractableUnset.Action -= OnHoverExit;

        // Grab / release
        rightInteractor.WhenInteractableSelected.Action -= OnSelect;
        rightInteractor.WhenInteractableUnselected.Action -= OnUnselect;
    }

    void Left_Selected()
    {
        //Debug.LogError("LeftSelected");

        LeftSelected?.Invoke(leftCursor);
    }

    void Left_UnSelected()
    {
        //Debug.LogError("LeftUnSelected");

        LeftSelected?.Invoke(null);
    }

    void Right_Selected()
    {
        //Debug.LogError("RightSelected");

        RightSelected?.Invoke(rightCursor);
    }

    void Right_UnSelected()
    {
        //Debug.LogError("RightUnSelected");

        RightSelected?.Invoke(null);
    }

    private void OnHoverEnter(DistanceHandGrabInteractable interactable)
    {
        //Debug.Log($"Hover start: {interactable.transform.name}");

        DistanceHandGrabHover?.Invoke();
    }

    private void OnHoverExit(DistanceHandGrabInteractable interactable)
    {
        //Debug.Log($"Hover end: {interactable.name}");
    }

    private void OnSelect(DistanceHandGrabInteractable interactable)
    {
        //Debug.Log($"Grabbed: {interactable.name}");

        DistanceHandGrabSelected?.Invoke(interactable.transform);
    }

    private void OnUnselect(DistanceHandGrabInteractable interactable)
    {
        //Debug.Log($"Released: {interactable.name}");

        DistanceHandGrabSelected?.Invoke(null);
    }
}