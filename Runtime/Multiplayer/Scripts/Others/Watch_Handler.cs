using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Watch_Handler : MonoBehaviour
{
    private static readonly WaitForSeconds waitForSeconds = new(1f);
    public static Watch_Handler Instance;

    [SerializeField] TextMeshProUGUI timerTxt;
    [SerializeField] TextMeshProUGUI sessionTxt;
    [SerializeField] TextMeshProUGUI batteryTxt;
    [SerializeField] Image batteryImg;

    private float lastBatteryValue = -1f; // For detecting changes
    private float sessionStartTime;

    Coroutine coroutine;

    void Awake()
    {
        Instance = this;
        sessionStartTime = Time.time; // start counting when enabled
    }

    void OnEnable()
    {
        coroutine = StartCoroutine(UpdateClock());
    }

    void OnDisable()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }

    IEnumerator UpdateClock()
    {
        while (true)
        {
            // Clock
            System.DateTime now = System.DateTime.Now;
            timerTxt.text = now.ToString("hh:mm:ss");

            // Session timer
            float elapsed = Time.time - sessionStartTime;
            System.TimeSpan t = System.TimeSpan.FromSeconds(elapsed);
            sessionTxt.text = $"Session:\n{t:hh\\:mm\\:ss}";

            // Battery value
            float batteryLevel = Mathf.Clamp01(SystemInfo.batteryLevel); // 0 to 1
            float batteryPercent = batteryLevel * 100f;
            batteryTxt.text = $"{batteryPercent:0}%";

            // Only tween if value changed significantly
            if (Mathf.Abs(batteryLevel - lastBatteryValue) > 0.01f)
            {
                lastBatteryValue = batteryLevel;

                // Decide target color based on percentage
                Color targetColor;
                if (batteryPercent > 50f)
                    targetColor = Color.green;
                else if (batteryPercent > 20f)
                    targetColor = Color.yellow;
                else
                    targetColor = Color.red;

                // Tween color
                batteryImg.DOColor(targetColor, 0.25f);

                // Tween fill amount
                batteryImg.DOFillAmount(batteryLevel, 0.25f);
            }

            yield return waitForSeconds; // Update once per second
        }
    }

    public void MenuClick()
    {
        Multiplayer_SoundManager.Instance.PlayClick();

        if (Multiplayer_UIManager.Instance.menu_Panel.IsOpen)
        {
            Multiplayer_UIManager.Instance.menu_Panel.Close();
        }
        else
        {
            //Multiplayer_UIManager.Instance.ResetMenuCanvas();
            
            Multiplayer_UIManager.Instance.menu_Panel.Open();
        }
    }
}