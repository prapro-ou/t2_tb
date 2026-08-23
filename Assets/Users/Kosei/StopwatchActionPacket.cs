using System;

public enum StopwatchActionType
{
    ToggleTimer,  // 操作側 -> タイマー側 : タイマーの開始/停止
    SyncResult,   // タイマー側 -> 操作側 : 1問題ごとの成否判定 (SUCCESS / FAILED)
    ResetNext,    // タイマー側 -> 操作側 : 次の問題へのリセット指示
    SyncClear     // タイマー側 -> 操作側 : 全問題クリア通知 (CLEAR)
}

[Serializable]
public struct StopwatchModuleActionPacket : IPacketType
{
    public StopwatchActionType ActionType { get; set; }
    public bool IsRunning { get; set; }  // ToggleTimer用
    public bool IsSuccess { get; set; }  // SyncResult用
}