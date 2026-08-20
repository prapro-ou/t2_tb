using UnityEngine;

public class StopwatchPlayerToolsOrchestrator 
    : ModuleToolsOrchestratorIndividual<StopwatchModuleSettingData>
{
    [SerializeField] private GameData gameData;
    [SerializeField] private OperatorGameManager operatorGameManager;

    // 単体テスト用：シーン実行時に自動でダミーデータを読み込む
    private void Start()
    {
        /*var dummySetting = new StopwatchModuleSettingData();
    
        // 値を直接代入して 0 を回避
        dummySetting.minTargetTime = 4.5f;
        dummySetting.maxTargetTime = 5.0f;
        dummySetting.tolerance = 0.25f;
        dummySetting.questionCount = 3;

        Debug.Log("[単体テスト] Start から InitializeModule を呼び出します");
        InitializeModule(dummySetting);
        */
    }

    // Inspectorの「On Initialize」イベントおよびStartから呼び出される初期化メソッド
    public void InitializeModule(StopwatchModuleSettingData settingData)
    {
        if (gameData != null)
        {
            gameData.Initialize(settingData); // 設定データをGameDataにセット
        }

        if (operatorGameManager != null)
        {
            operatorGameManager.StartGame(); // ゲームスタート
        }
    }

    // OperatorGameManager から呼び出してパケットを送信する
    public void SendStart()
    {
        var packet = new StopwatchActionPacket { ActionType = StopwatchActionType.Start };
        
        try
        {
            SendModuleInfoPacket(ModuleVersionEnum.A, packet);
        }
        catch (System.NullReferenceException)
        {
            Debug.LogWarning("[単体テスト] 通信基盤がないため SendStart のパケット送信をスキップしました");
        }
    }

    public void SendStop()
    {
        var packet = new StopwatchActionPacket { ActionType = StopwatchActionType.Stop };
        
        try
        {
            SendModuleInfoPacket(ModuleVersionEnum.A, packet);
        }
        catch (System.NullReferenceException)
        {
            Debug.LogWarning("[単体テスト] 通信基盤がないため SendStop のパケット送信をスキップしました");
        }
    }
}