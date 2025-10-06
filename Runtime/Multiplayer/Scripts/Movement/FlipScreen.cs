using System.Collections;
using Fusion;
using UnityEngine;

public class FlipScreen : MonoBehaviour
{
    [SerializeField] Transform rootTransform;
    NetworkObject networkObjectTransform;
    private Coroutine waitAuthorityRoutine;

    void Start()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher)
            gameObject.SetActive(false);

        networkObjectTransform = rootTransform.GetComponent<NetworkObject>();
    }

    public void FlipScreenClick()
    {
        Multiplayer_SoundManager.Instance.PlayClick();

        //Debug.Log("FlipScreenClick");

        if (networkObjectTransform != null)
        {
            if (networkObjectTransform.HasStateAuthority)
            {
                rootTransform.Rotate(0, 180, 0);
            }
            else
            {
                // start polling until we get authority
                if (waitAuthorityRoutine != null) StopCoroutine(waitAuthorityRoutine);
                waitAuthorityRoutine = StartCoroutine(WaitForAuthority());
            }
        }
        else
        {
            rootTransform.Rotate(0, 180, 0);
        }
    }

    private IEnumerator WaitForAuthority()
    {
        networkObjectTransform.RequestStateAuthority();

        yield return new WaitUntil(() => networkObjectTransform.HasStateAuthority);

        // ✅ continue original click flow once we got authority
        rootTransform.Rotate(0, 180, 0);

        waitAuthorityRoutine = null;
    }
}