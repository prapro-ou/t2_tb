using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timerText;

    private float currentTime = 0f;
    private bool isRunning = false;

    void Start()
    {
        currentTime = 0f;
        UpdateTimerText();
    }

    void Update()
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
        currentTime = 0f;
        isRunning = true;
        UpdateTimerText();
    }

    /// <summary>
    /// タイマー停止
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
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
    /// タイマーが動作中か取得
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
        timerText.text = currentTime.ToString("F2");
    }
}