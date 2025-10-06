using Oculus.Interaction;
using UnityEngine;

[RequireComponent(typeof(Grabbable))]
public class WhoCanGrab : MonoBehaviour
{
    [SerializeField] PlayerMode playerMode = PlayerMode.Teacher;

    Grabbable[] grabbables = new Grabbable[0];

    private void Awake()
    {
        if (grabbables.Length == 0)
        {
            grabbables = GetComponentsInChildren<Grabbable>();
        }
    }

    void Start()
    {
        if (FusionLobbyManager.Instance.playerMode != playerMode)
        {
            if (grabbables.Length != 0)
            {
                foreach (Grabbable grabbable in grabbables)
                    grabbable.MaxGrabPoints = 0;
            }
        }
    }
}