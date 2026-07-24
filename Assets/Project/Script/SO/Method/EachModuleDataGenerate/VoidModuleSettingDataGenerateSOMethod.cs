public class VoidModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<VoidModuleSettingData>
{
    protected override VoidModuleSettingData GeneratePacketType()
    {
        return new VoidModuleSettingData();
    }
}