using UnityEngine;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;

public class StopwatchModuleActionOrchestrator : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private TimerGameManager _timerGameManager;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(StopwatchModuleSettingData moduleSettingData)
    {
        EOSP2PMethod.RegisterListener<StopwatchModuleActionPacket>(
            OnActionReceived
        );
    }

    /// <summary>
    /// START / STOP受信
    /// </summary>
    private void OnActionReceived(
        ProductUserId remoteUserId,
        string socketName,
        StopwatchModuleActionPacket packet)
    {
        if (_timerGameManager == null)
        {
            Debug.LogError(
                "StopwatchModuleActionOrchestrator: TimerGameManager が設定されていません。"
            );

            return;
        }

        switch (packet.Action)
        {
            case StopwatchActionEnum.Start:
                _timerGameManager.ReceiveStart();
                break;

            case StopwatchActionEnum.Stop:
                _timerGameManager.ReceiveStop();
                break;

            default:
                Debug.LogError(
                    $"StopwatchModuleActionOrchestrator: 不明なActionです: {packet.Action}"
                );
                break;
        }
    }
}