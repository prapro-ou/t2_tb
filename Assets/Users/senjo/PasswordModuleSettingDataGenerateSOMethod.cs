public class PasswordModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<PasswordModuleSettingData>
{
    protected override PasswordModuleSettingData GeneratePacketType()
    {
        return new PasswordModuleSettingData();
    }
}