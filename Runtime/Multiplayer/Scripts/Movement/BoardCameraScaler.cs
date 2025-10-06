using UnityEngine;

public class BoardCameraScaler : MonoBehaviour
{
    public Camera targetCamera;   // Assign your orthographic camera
    public Transform parent;      // Parent whose scale you want to track

    [Header("Scale Range")]
    public float minScale = 0.5f;
    public float maxScale = 3f;

    [Header("Camera Size")]
    public float baseScale = 1f;      // scale where we know the reference size
    public float baseOrthoSize = 0.5f; // at scale=1, orthoSize=0.5

    private void Update()
    {
        if (targetCamera == null || parent == null) return;

        float scale = parent.localScale.x; // assuming uniform scaling
        scale = Mathf.Clamp(scale, minScale, maxScale);

        // proportional mapping: orthoSize = baseOrthoSize * (scale / baseScale)
        float orthoSize = baseOrthoSize * (scale / baseScale);

        targetCamera.orthographicSize = orthoSize;
    }
}