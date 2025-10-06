using UnityEngine;
using UnityEngine.Events;

public class ResetPlayerByTeacher : MonoBehaviour
{
    [SerializeField] Transform studentPosition;
    [SerializeField] Transform distanceGrab;
    [SerializeField] GameObject[] childs;

    public UnityEvent OnGrab;
    public UnityEvent OnHover;

    bool isMoving;

    void Awake()
    {
        if (FusionLobbyManager.Instance.playerMode == PlayerMode.Student)
        {
            foreach (GameObject child in childs)
            {
                child.SetActive(false);
            }
        }
    }

    void OnEnable()
    {
        FusionLobbyManager.ResetPlayer += OnResetPlayer;

        Invoke(nameof(RegisterEvent), 1);
    }

    void RegisterEvent()
    {
        ControllerButtonsManager.DistanceHandGrabSelected += DistanceGrabEvent;
        ControllerButtonsManager.DistanceHandGrabHover += DistanceHandHover;
    }

    void OnDisable()
    {
        FusionLobbyManager.ResetPlayer -= OnResetPlayer;
        ControllerButtonsManager.DistanceHandGrabSelected -= DistanceGrabEvent;
        ControllerButtonsManager.DistanceHandGrabHover -= DistanceHandHover;
    }

    private void OnResetPlayer()
    {
        if (FusionLobbyManager.Instance.playerMode == PlayerMode.Student)
        {
            if (studentPosition == null)
            {
                Debug.LogError("Student position is not assigned in ResetPlayerByTeacher script.");
                return;
            }
            //Debug.Log("OnResetPlayer");
            FusionLobbyManager.Instance.RepositionMetaLocalPlayer(studentPosition);
        }
    }

    void DistanceHandHover()
    {
        OnHover?.Invoke();
    }

    void DistanceGrabEvent(Transform pointer)
    {
        //Debug.Log("DistanceGrabEvent - " + (distanceGrab == pointer));

        if (distanceGrab == pointer)
        {
            FusionLobbyManager.Instance.StopLocomotion(false);
            OnGrab?.Invoke();
            isMoving = true;
        }
        else
        {
            FusionLobbyManager.Instance.StopLocomotion(true);

            if (isMoving)
            {
                //Debug.Log("RPC_ResetAllPlayers - Called");
                Vector3 pos = studentPosition.position;
                pos.x = RoundTo3(pos.x);
                pos.z = RoundTo3(pos.z);
                Vector3 rot = studentPosition.eulerAngles;
                rot.y = RoundTo3(rot.y);
                studentPosition.position = pos;
                studentPosition.eulerAngles = rot;
                //Debug.Log($"RepositionMetaLocalPlayer to {studentPosition.position}, {studentPosition.eulerAngles}" + " - " + studentPosition.name);
                Invoke(nameof(ResetPlayers), 0.25f);
                isMoving = false;
            }
        }
    }

    void ResetPlayers()
    {
        NetworkSyncVariables.Instance.RPC_ResetAllPlayers();
    }

    float RoundTo3(float value)
    {
        return Mathf.Round(value * 1000f) / 1000f;
    }
}