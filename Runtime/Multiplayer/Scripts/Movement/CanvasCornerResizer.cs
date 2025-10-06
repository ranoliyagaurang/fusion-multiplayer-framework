using Fusion;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class CanvasCornerResizer : NetworkBehaviour
{
    [Header("Target")]
    NetworkObject targetCanvasParentNetObj; // 3D network object
    public Transform targetCanvasParent;   // 3D transform (not RectTransform)
    public Transform targetCanvas;         // 3D transform (not RectTransform)

    [Header("Settings")]
    public float minScale = 0.5f;
    public float maxScale = 3f;

    [Header("Mode")]
    public bool isMoveHandle = false; // true = move handle (side), false = resize handle (corner)
    [Range(10f, 89f)] public float pitchClamp = 50f; // vertical clamp to prevent flips

    [Header("Joystick Move Settings")]
    public float speed = 750f;

    [Header("Restrictions")]
    public Collider roomBounds;   // box or mesh collider of the room
    public float minDistanceFromCamera = 0.5f; // prevent canvas from coming too close

    [Header("Events")]
    public UnityEvent OnClick;

    Transform pointerRef;
    private Vector3 startPointerPos;
    private Vector3 startScale;
    private bool isSelected = false;
    private bool isLeftSelected = false;

    // --- move state ---
    private float orbitRadius;
    private Vector3 grabOffsetLocal;   // offset from grab point to canvas center (in cam local space)
    Vector2 joystickValue;
    Camera cam;
    private Vector3 prevWorldPos;

    // authority helpers
    private Coroutine waitAuthorityRoutine;

    void OnEnable()
    {
        ControllerButtonsManager.LeftSelected += OnLeftSelected;
        ControllerButtonsManager.RightSelected += OnRightSelected;
        ControllerButtonsManager.OnLeftStickChanged += OnLeftStickChanged;
        ControllerButtonsManager.OnRightStickChanged += OnRightStickChanged;
    }

    void OnDisable()
    {
        ControllerButtonsManager.LeftSelected -= OnLeftSelected;
        ControllerButtonsManager.RightSelected -= OnRightSelected;
        ControllerButtonsManager.OnLeftStickChanged -= OnLeftStickChanged;
        ControllerButtonsManager.OnRightStickChanged -= OnRightStickChanged;
    }

    void Start()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher)
            gameObject.SetActive(false);

        cam = Camera.main;

        GetRoomBounds();

        prevWorldPos = targetCanvas.position;

        targetCanvasParentNetObj = targetCanvasParent.GetComponent<NetworkObject>();
    }

    public void GetRoomBounds()
    {
        if (roomBounds == null)
        {
            RoomBounds rb = FindAnyObjectByType<RoomBounds>();
            if (rb != null)
                roomBounds = rb.roomCollider;
        }
    }

    void Update()
    {
        if (pointerRef == null || !isSelected) return;

        if (targetCanvasParentNetObj != null)
        {
            if (!targetCanvasParentNetObj.HasStateAuthority) return;
        }

        if (Mathf.Abs(joystickValue.y) > 0.1f)
        {
            // ✅ apply movement directly in local space (z = forward/backward)
            Vector3 localPos = targetCanvas.localPosition;
            //Debug.LogError("joystickValue.y: " + joystickValue.y);
            //Debug.LogError("localPos: " + localPos);
            localPos.z += joystickValue.y * speed * Time.deltaTime;
            targetCanvas.localPosition = localPos;
            //Debug.LogError("targetCanvas: " + localPos);

            // Clamp child in world space
            if (roomBounds != null)
            {
                Vector3 worldPos = targetCanvas.position;
                worldPos = ClampToRoom(worldPos);

                // ✅ restrict joystick from moving canvas closer than min distance
                if (cam != null)
                {
                    Vector3 camPos = cam.transform.position;
                    Vector3 toCanvas = worldPos - camPos;
                    float forwardDist = Vector3.Dot(toCanvas, cam.transform.forward);

                    if (forwardDist < minDistanceFromCamera)
                    {
                        // 🚫 cancel forward motion — keep it at the last safe position
                        worldPos = prevWorldPos;
                    }
                    else
                    {
                        // ✅ update last safe position
                        prevWorldPos = worldPos;
                    }
                }

                // convert back into local relative to parent
                targetCanvas.localPosition = targetCanvasParent.InverseTransformPoint(worldPos);
            }
        }
    }

    public void OnPointerDown()
    {
        if (pointerRef == null) return;

        OnClick?.Invoke();

        if (targetCanvasParentNetObj != null)
        {
            if (!targetCanvasParentNetObj.HasStateAuthority)
            {
                // 🚩 store event and request authority
                targetCanvasParentNetObj.RequestStateAuthority();

                // start polling until we get authority
                if (waitAuthorityRoutine != null) StopCoroutine(waitAuthorityRoutine);
                waitAuthorityRoutine = StartCoroutine(WaitForAuthority());
                return;
            }
        }

        BeginSelection();
    }

    private IEnumerator WaitForAuthority()
    {
        yield return new WaitUntil(() => targetCanvasParentNetObj.HasStateAuthority);

        // 🚩 wait at least 1–2 network ticks to get the latest synced transform
        yield return new WaitForFixedUpdate();  // waits one physics tick
        yield return null;                      // +1 frame if needed

        // ✅ continue original click flow once we got authority
        BeginSelection();

        waitAuthorityRoutine = null;
    }

    private void BeginSelection()
    {
        if (pointerRef == null) return;

        // ✅ Prevent snapping on first grab after authority transfer
        RecenterCanvas();

        startPointerPos = pointerRef.position;
        startScale = targetCanvas.localScale;

        if (isMoveHandle)
        {
            if (!cam) return;

            orbitRadius = Vector3.Distance(targetCanvasParent.position, cam.transform.position);

            Vector3 grabDirWorld = (pointerRef.position - cam.transform.position).normalized;
            Vector3 grabPointWorld = cam.transform.position + grabDirWorld * orbitRadius;
            grabOffsetLocal = Quaternion.Inverse(cam.transform.rotation) * (targetCanvasParent.position - grabPointWorld);

            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
        }
        
        isSelected = true;
    }

    public void OnDrag()
    {
        if (pointerRef == null) return;

        if (targetCanvasParentNetObj != null)
        {
            if (!targetCanvasParentNetObj.HasStateAuthority) return;
        }

        if (!isSelected) return;

        if (isMoveHandle)
        {
            if (!cam) return;

            // pointer orbit
            Vector3 dirLocal = Quaternion.Inverse(cam.transform.rotation) *
                               (pointerRef.position - cam.transform.position).normalized;

            // 🔑 Don’t clamp here — use full pointer dir
            Vector3 grabPointWorld = cam.transform.position + cam.transform.rotation * dirLocal * orbitRadius;
            Vector3 newCanvasPos = grabPointWorld + cam.transform.rotation * grabOffsetLocal;

            // ✅ Clamp relative angle instead of position
            Vector3 offset = newCanvasPos - cam.transform.position;
            float pitch = Vector3.SignedAngle(Vector3.ProjectOnPlane(offset, Vector3.up), offset, cam.transform.right);

            // hard clamp pitch so it never goes overhead
            if (pitch > pitchClamp)
                offset = Quaternion.AngleAxis(pitchClamp - pitch, cam.transform.right) * offset;
            else if (pitch < -pitchClamp)
                offset = Quaternion.AngleAxis(-pitchClamp - pitch, cam.transform.right) * offset;

            newCanvasPos = cam.transform.position + offset;

            // ✅ Restrict inside room
            newCanvasPos = ClampToRoom(newCanvasPos);

            targetCanvasParent.position = newCanvasPos;

            // face camera
            Vector3 lookDir = cam.transform.position - targetCanvasParent.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 1e-6f)
                targetCanvasParent.rotation = Quaternion.LookRotation(-lookDir.normalized, Vector3.up);
        }
        else
        {
            // scaling (unchanged)
            float startDist = Vector3.Distance(targetCanvasParent.position, startPointerPos);
            float currentDist = Vector3.Distance(targetCanvasParent.position, pointerRef.position);

            if (startDist > 0.0001f)
            {
                float scaleFactor = currentDist / startDist;
                Vector3 newScale = startScale * scaleFactor;
                float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);
                clamped = RoundTo3(clamped);
                targetCanvas.localScale = new Vector3(clamped, clamped, clamped);
            }
        }
    }

    public void OnPointerUp()
    {
        isSelected = false;

        // ✅ Re-center when releasing grab
        RecenterCanvas();
    }

    private Vector3 ClampToRoom(Vector3 pos)
    {
        if (roomBounds == null) return pos;

        Bounds b = roomBounds.bounds;
        pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
        pos.y = Mathf.Clamp(pos.y, b.min.y, b.max.y);
        pos.z = Mathf.Clamp(pos.z, b.min.z, b.max.z);
        return pos;
    }

    float RoundTo3(float value)
    {
        return Mathf.Round(value * 1000f) / 1000f;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (roomBounds != null)
        {
            // Draw the room bounds in cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(roomBounds.bounds.center, roomBounds.bounds.size);
        }

        if (targetCanvasParent != null)
        {
            // Draw the parent position in green
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetCanvasParent.position, 0.05f);
        }

        if (targetCanvas != null)
        {
            // Draw the child canvas position in yellow
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetCanvas.position, 0.04f);
        }
    }
#endif

    // input events
    void OnLeftSelected(Transform pointer) { pointerRef = pointer; isLeftSelected = true; }
    void OnRightSelected(Transform pointer) { pointerRef = pointer; isLeftSelected = false; }
    void OnLeftStickChanged(Vector2 value) { if (isLeftSelected) joystickValue = value; }
    void OnRightStickChanged(Vector2 value) { if (!isLeftSelected) joystickValue = value; }

    #region Authority Helpers

    /// <summary>
    /// Re-center the child canvas so that world position stays the same
    /// but local position is reset. Prevents snapping when new teacher grabs
    /// after authority transfer.
    /// </summary>
    private void RecenterCanvas()
    {
        if (targetCanvas != null && targetCanvas.localPosition != Vector3.zero)
        {
            // move parent so child keeps same world position
            Vector3 childWorld = targetCanvas.position;
            targetCanvasParent.position = childWorld;
            targetCanvas.localPosition = Vector3.zero;
        }
    }

    #endregion
}