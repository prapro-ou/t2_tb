using UnityEngine;

public class TimerGameManager : MonoBehaviour
{
    [Header("Manager")]
    public TimerManager timerManager;

    // 最後に計測した時間
    private float measuredTime = 0f;

    /// <summary>
    /// STARTを受信
    /// </summary>
    public void ReceiveStart()
    {
        Debug.Log("ReceiveStart");

        timerManager.ResetTimer();
        timerManager.StartTimer();
    }

    /// <summary>
    /// STOPを受信
    /// </summary>
    public void ReceiveStop()
    {
        timerManager.StopTimer();

        measuredTime =
            timerManager.GetCurrentTime();
    }

    /// <summary>
    /// 最後に計測した時間
    /// </summary>
    public float GetMeasuredTime()
    {
        return measuredTime;
    }

    /// <summary>
    /// リセット
    /// </summary>
    public void ResetTimer()
    {
        measuredTime = 0f;
        timerManager.ResetTimer();
    }
}