using System.Collections;
using Fusion;
using Fusion.Addons.Drawing;
using UnityEngine;

public class FollowTarget : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnChangedGrab))]
    public bool IsGrabbed { get; set; }
    Transform penTarget;
    NetworkObject penTargetNetworkObject;

    void Start()
    {
        if (Board.Instance != null)
        {
            penTarget = Board.Instance.penTarget;
        }
        
        penTarget.SetPositionAndRotation(transform.position, transform.rotation);

        penTargetNetworkObject = penTarget.root.GetComponent<NetworkObject>();
    }

    public override void Spawned()
    {
        base.Spawned();
    }

    void LateUpdate()
    {
        if (!Object.HasStateAuthority) return;

        if (IsGrabbed) return;

        transform.SetPositionAndRotation(penTarget.position, penTarget.rotation);
    }

    public void GrabPen()
    {
        if (!Object.HasStateAuthority)
        {
            StartCoroutine(SetVariable(true));
            return;
        }

        IsGrabbed = true;

        if(!penTargetNetworkObject.HasStateAuthority)
            penTargetNetworkObject.RequestStateAuthority();
    }

    public void ReleasePen()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        IsGrabbed = false;

        penTarget.SetPositionAndRotation(transform.position, transform.rotation);
    }

    void OnChangedGrab()
    {

    }

    private IEnumerator SetVariable(bool active)
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
            while (!Object.HasStateAuthority)
            {
                yield return null;
            }
        }

        yield return null;

        IsGrabbed = active;
        
        if(!penTargetNetworkObject.HasStateAuthority)
            penTargetNetworkObject.RequestStateAuthority();
    }
}