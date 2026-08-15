public class TimeModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<TimeModuleSettingData>
{
    protected override TimeModuleSettingData GeneratePacketType()
    {
        return new TimeModuleSettingData();
    }
}