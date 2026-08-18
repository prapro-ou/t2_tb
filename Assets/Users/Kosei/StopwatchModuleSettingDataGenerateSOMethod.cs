public class StopwatchModuleSettingDataGenerateSOMethod
    : TypeModuleSettingDataGenerateSOMethod<StopwatchModuleSettingData>
{
    protected override StopwatchModuleSettingData GeneratePacketType()
    {
        return new StopwatchModuleSettingData
            {
            minTargetTime = 2.0f,
            maxTargetTime = 8.0f,
            tolerance = 0.25f,
            questionCount = 3
        };
    }
}