using UnityEngine;

public class LocalNetworkManager : MonoBehaviour
{
    [Header("Manager")]
    public TimerGameManager timerGameManager;

    /// <summary>
    /// START送信
    /// </summary>
    public void SendStart()
    {
        timerGameManager.ReceiveStart();
    }

    /// <summary>
    /// STOP送信
    /// </summary>
    public void SendStop()
    {
        timerGameManager.ReceiveStop();
    }

    /// <summary>
    /// 計測時間取得
    /// </summary>
    public float ReceiveTime()
    {
        return timerGameManager.GetMeasuredTime();
    }

    /// <summary>
    /// タイマーリセット
    /// </summary>
    public void ResetTimer()
    {
        timerGameManager.ResetTimer();
    }
}