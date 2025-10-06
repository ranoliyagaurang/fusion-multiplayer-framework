using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Collections;
using UnityEngine.Events;

[System.Serializable]   // Required to show in Inspector
public class StringEvent : UnityEvent<string> { }

public class GrabWithGhostHand : NetworkBehaviour
{
    [Header("NetworkVariables")]
    [Networked, OnChangedRender(nameof(OnChangedHolding))]
    public bool IsHolding { get; set; }
    [Networked] public string HandName { get; set; }

    [Header("LocalVariables")]
    public string localUserLayer = "InvisibleForLocalPlayer";
    [SerializeField] SkinnedMeshRenderer ghostLeftHand;
    [SerializeField] SkinnedMeshRenderer ghostRightHand;
    [SerializeField] bool useGravity = false;
    public StringEvent releasedRPCCallback;

    readonly List<string> handId = new();
    private Rigidbody _rigidbody;

    private void Start()
    {
        HideGhostHand("LeftHand", true);
        HideGhostHand("RightHand", true);

        if (useGravity)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
    }

    void OnChangedHolding()
    {
        if (useGravity)
        {
            _rigidbody.isKinematic = IsHolding;
            _rigidbody.useGravity = !IsHolding;
        }
    }

    void Grabbed(string handName)
    {
        //Debug.Log("Grabbed RPC");

        HideGhostHand(handName, false);

        handId.Add(handName);
    }

    void Released(string handName)
    {
        //Debug.Log("Released RPC");

        HideGhostHand(handName, true);

        if (handId.Contains(handName))
            handId.Remove(handName);

        releasedRPCCallback?.Invoke(handName);
    }

    public void GrabbedObject(string handName)
    {
        //Debug.Log("GrabbedObject");

        handId.Add(handName);

        IsHolding = true;

        UpdateNetworkVariable(true, handName);

        RPC_Grabbed(handName);

        if (handName.Equals("LeftHand"))
        {
            NetworkSyncVariables.Instance.ActiveLeftHand(false);
        }
        else if (handName.Equals("RightHand"))
        {
            NetworkSyncVariables.Instance.ActiveRightHand(false);
        }
    }

    public void ReleasedObject(string handName)
    {
        //Debug.Log("ReleasedObject");

        if (handId.Contains(handName))
            handId.Remove(handName);

        if (handId.Count == 0)
        {
            IsHolding = false;

            UpdateNetworkVariable(false, string.Empty);
        }

        if (handName.Equals("LeftHand"))
        {
            NetworkSyncVariables.Instance.ActiveLeftHand(true);
        }
        else if (handName.Equals("RightHand"))
        {
            NetworkSyncVariables.Instance.ActiveRightHand(true);
        }

        RPC_Released(handName);
    }

    public void HideGhostHand(string handName, bool hide)
    {
        //Debug.Log("HideGhostHand - " + gameObject.name + " : " + handName + " : " + hide);

        if (hide)
        {
            if (handName.Equals("LeftHand"))
            {
                ghostLeftHand.enabled = false;
            }
            else
            {
                ghostRightHand.enabled = false;
            }
        }
        else
        {
            if (handName.Equals("LeftHand"))
            {
                ghostLeftHand.enabled = true;
            }
            else
            {
                ghostRightHand.enabled = true;
            }
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        //Debug.Log("GhostHand Object Spawned - " + IsHolding);

        if (IsHolding)
        {
            Grabbed(HandName);
            OnChangedHolding();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false, TickAligned = false)]
    void RPC_Grabbed(string handName, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }
        Grabbed(handName);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false, TickAligned = false)]
    void RPC_Released(string handName, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }
        Released(handName);
    }

    void UpdateNetworkVariable(bool active, string handName)
    {
        StartCoroutine(SetNetworkVariable(active, handName));
    }

    IEnumerator SetNetworkVariable(bool active, string handName)
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

        IsHolding = active;
        HandName = handName;
    }
}