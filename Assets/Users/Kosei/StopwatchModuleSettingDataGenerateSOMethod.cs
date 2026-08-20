using UnityEngine;

[CreateAssetMenu(fileName = "StopwatchModuleSettingDataGenerateSOMethod", menuName = "Module/Setting/Stopwatch")]
public class StopwatchModuleSettingDataGenerateSOMethod 
    : TypeModuleSettingDataGenerateSOMethod<StopwatchModuleSettingData>
{
    [SerializeField] private float minTargetTime = 2.0f;
    [SerializeField] private float maxTargetTime = 8.0f;
    [SerializeField] private float tolerance = 0.25f;
    [SerializeField] private int questionCount = 3;

    protected override StopwatchModuleSettingData GeneratePacketType()
    {
        return new StopwatchModuleSettingData
        {
            minTargetTime = minTargetTime,
            maxTargetTime = maxTargetTime,
            tolerance = tolerance,
            questionCount = questionCount
        };
    }
}