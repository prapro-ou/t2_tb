using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timerText;

    private float currentTime = 0f;
    private bool isRunning = false;

    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        currentTime += Time.deltaTime;

        UpdateTimerText();
    }

    /// <summary>
    /// タイマー開始
    /// </summary>
    public void StartTimer()
    {
        Debug.Log("Timer Start");

        currentTime = 0f;
        isRunning = true;

        UpdateTimerText();
    }

    /// <summary>
    /// タイマー停止
    /// </summary>
    public void StopTimer()
    {
        Debug.Log(
            $"Timer Stop : {currentTime:F2}"
        );

        isRunning = false;

        UpdateTimerText();
    }

    /// <summary>
    /// タイマーリセット
    /// </summary>
    public void ResetTimer()
    {
        currentTime = 0f;
        isRunning = false;

        UpdateTimerText();
    }

    /// <summary>
    /// 現在時間取得
    /// </summary>
    public float GetCurrentTime()
    {
        return currentTime;
    }

    /// <summary>
    /// タイマーが動作中か
    /// </summary>
    public bool IsRunning()
    {
        return isRunning;
    }

    /// <summary>
    /// タイマー表示更新
    /// </summary>
    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text =
                currentTime.ToString("F2");
        }
    }
}