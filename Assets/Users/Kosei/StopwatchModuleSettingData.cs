public struct StopwatchModuleSettingData : IModuleSettingData
{
    // 目標時間の最小値
    public float minTargetTime;

    // 目標時間の最大値
    public float maxTargetTime;

    // 成功判定の許容誤差
    public float tolerance;

    // 問題数
    public int questionCount;
}