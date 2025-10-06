using System.Collections;
using Fusion;
using UnityEngine;

public class ToolsManager : MonoBehaviour
{
    [SerializeField] GameObject resetToolBt;
    [SerializeField] NetworkObject[] tools;
    [SerializeField] GrabWithGhostHand[] grabWithGhostHands;
    [SerializeField] Vector3[] toolsPosition;
    [SerializeField] Vector3[] toolsRotation;

    void Start()
    {
        resetToolBt.SetActive(FusionLobbyManager.Instance.playerMode == PlayerMode.Teacher);

        toolsPosition = new Vector3[tools.Length];
        toolsRotation = new Vector3[tools.Length];

        grabWithGhostHands = new GrabWithGhostHand[tools.Length];

        for (int i = 0; i < tools.Length; i++)
        {
            toolsPosition[i] = tools[i].transform.localPosition;
            toolsRotation[i] = tools[i].transform.localEulerAngles;
            grabWithGhostHands[i] = tools[i].GetComponent<GrabWithGhostHand>();
        }
    }

    public void ResetAllToolsClick()
    {
        for (int i = 0; i < tools.Length; i++)
        {
            if (tools[i].HasStateAuthority && !grabWithGhostHands[i].IsHolding)
                tools[i].transform.SetLocalPositionAndRotation(toolsPosition[i], Quaternion.Euler(toolsRotation[i]));
            else if(!tools[i].HasStateAuthority && !grabWithGhostHands[i].IsHolding)
                StartCoroutine(ResetTool(tools[i], i));
        }

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    IEnumerator ResetTool(NetworkObject network, int index)
    {
        network.RequestStateAuthority();

        yield return new WaitUntil(() => network.HasStateAuthority);
            
        network.transform.SetLocalPositionAndRotation(toolsPosition[index], Quaternion.Euler(toolsRotation[index]));
    }
}