using UnityEngine;

public class StopwatchInstructorToolsOrchestrator 
    : ModuleToolsOrchestratorIndividual<StopwatchModuleSettingData>
{
    [SerializeField] private TimerGameManager timerGameManager;
    private string _listenerUuid;

    // Inspectorの「On Initialize」イベントから登録するメソッド
    public void InitializeModule(StopwatchModuleSettingData settingData)
    {
        // パケット受信登録
        _listenerUuid = ModuleDataRegisterListener<StopwatchActionPacket>(OnActionPacketReceived);
    }

    private void OnActionPacketReceived(ModuleVersionEnum version, StopwatchActionPacket packet)
    {
        if (timerGameManager == null) return;

        if (packet.ActionType == StopwatchActionType.Start)
        {
            timerGameManager.ReceiveStart(); // 既存のTimerGameManagerにスタートを伝える
        }
        else if (packet.ActionType == StopwatchActionType.Stop)
        {
            timerGameManager.ReceiveStop(); // 既存のTimerGameManagerにストップを伝える[cite: 2]
        }
    }

    // 成功・失敗時に既存コードやフレームワークに伝える
    public void OnRoundSuccess()
    {
        if (timerGameManager != null)
        {
            timerGameManager.SetSuccess();
        }
    }

    public void OnRoundFailed()
    {
        if (timerGameManager != null)
        {
            timerGameManager.SetFailed();
        }
    }

    public void OnAllQuestionsClear()
    {
        if (timerGameManager != null)
        {
            timerGameManager.SetClear();
        }
        ModuleSuccess(); // モジュール全体クリアを通知
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(_listenerUuid))
        {
            ModuleDataUnregisterListener(_listenerUuid);
        }
    }
}