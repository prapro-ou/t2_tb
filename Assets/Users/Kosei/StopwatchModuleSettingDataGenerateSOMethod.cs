using UnityEngine;

[CreateAssetMenu(fileName = "StopwatchModuleSettingDataGenerate", menuName = "SO/ModuleSettingData/Stopwatch")]
public class StopwatchModuleSettingDataGenerateSOMethod 
    : TypeModuleSettingDataGenerateSOMethod<StopwatchModuleSettingData>
{
    [Header("ストップウォッチの設定パラメータ")]
    [SerializeField] private float minTargetTime = 3.0f;
    [SerializeField] private float maxTargetTime = 8.0f;
    [SerializeField] private float tolerance = 0.5f;
    [SerializeField] private int questionCount = 3;

    protected override StopwatchModuleSettingData GeneratePacketType()
    {
        Debug.Log($"【StopwatchSO】データ生成開始: Target={minTargetTime}s~{maxTargetTime}s");

        return new StopwatchModuleSettingData
        {
            minTargetTime = this.minTargetTime,
            maxTargetTime = this.maxTargetTime,
            tolerance = this.tolerance,
            questionCount = this.questionCount
        };
    }
}