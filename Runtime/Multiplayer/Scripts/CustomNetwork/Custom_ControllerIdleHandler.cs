using UnityEngine;

public class Custom_ControllerIdleHandler : MonoBehaviour
{
    [Header("Assign Controller Models")]
    public GameObject leftControllerModel;
    public GameObject rightControllerModel;

    [Header("Settings")]
    public int idleLayer = 8;   // Layer to assign when idle
    public int activeLayer = 0; // Layer to assign when active

    private void OnEnable()
    {
        OVRManager.TrackingAcquired += OnTrackingAcquired;
        OVRManager.TrackingLost += OnTrackingLost;
    }

    private void OnDisable()
    {
        OVRManager.TrackingAcquired -= OnTrackingAcquired;
        OVRManager.TrackingLost -= OnTrackingLost;
    }

    private void OnTrackingAcquired()
    {
        // Tracking restored → show & reset layer
        if (leftControllerModel)  leftControllerModel.layer  = activeLayer;
        if (rightControllerModel) rightControllerModel.layer = activeLayer;

        if (leftControllerModel)  leftControllerModel.SetActive(true);
        if (rightControllerModel) rightControllerModel.SetActive(true);
    }

    private void OnTrackingLost()
    {
        // Tracking lost → hide or assign to idle layer
        if (leftControllerModel)  leftControllerModel.layer  = idleLayer;
        if (rightControllerModel) rightControllerModel.layer = idleLayer;

        if (leftControllerModel)  leftControllerModel.SetActive(false);
        if (rightControllerModel) rightControllerModel.SetActive(false);
    }
}