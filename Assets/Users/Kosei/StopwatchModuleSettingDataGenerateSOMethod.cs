public class StopwatchModuleSettingDataGenerateSOMethod
    : TypeModuleSettingDataGenerateSOMethod<StopwatchModuleSettingData>
{
    protected override StopwatchModuleSettingData GeneratePacketType()
    {
        return new StopwatchModuleSettingData();
    }
}