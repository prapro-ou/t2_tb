using UnityEngine;

public class StopwatchReceiver : MonoBehaviour
{
    [Header("Module Tools")]
    [SerializeField]
    private StopwatchModuleToolsOrchestrator stopwatchTools;

    [Header("Timer")]
    [SerializeField]
    private TimerGameManager timerGameManager;

    private string actionListenerUUID;

    /// <summary>
    /// 通信受信登録
    /// </summary>
    public void Initialize(StopwatchModuleSettingData moduleSettingData)
    {
        if (stopwatchTools == null)
        {
            Debug.LogError(
                "StopwatchModuleToolsOrchestrator is not assigned."
            );
            return;
        }

        if (timerGameManager == null)
        {
            Debug.LogError(
                "TimerGameManager is not assigned."
            );
            return;
        }

        actionListenerUUID =
            stopwatchTools.RegisterActionListener(
                OnActionReceived
            );

        Debug.Log(
            "Stopwatch Action Listener Registered"
        );
    }

    /// <summary>
    /// START / STOPを受信
    /// </summary>
    private void OnActionReceived(
        ModuleVersionEnum moduleVersion,
        bool isStart)
    {
        Debug.Log(
            $"Action Received : " +
            $"Version={moduleVersion}, " +
            $"IsStart={isStart}"
        );

        if (isStart)
        {
            timerGameManager.ReceiveStart();
        }
        else
        {
            timerGameManager.ReceiveStop();

            float measuredTime =
                timerGameManager.GetMeasuredTime();

            Debug.Log(
                $"Measured Time : {measuredTime:F2}"
            );

            stopwatchTools.SendTime(
                moduleVersion,
                measuredTime
            );
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(actionListenerUUID))
        {
            stopwatchTools.ModuleDataUnregisterListener(
                actionListenerUUID
            );

            actionListenerUUID = string.Empty;
        }
    }
}