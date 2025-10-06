using TMPro;
using UnityEngine;

public class LogsView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI msgText;

    public void Off()
    {
        gameObject.SetActive(false);
    }

    public void Bind(string msg)
    {
        msgText.text = msg;

        gameObject.SetActive(true);
    }
}