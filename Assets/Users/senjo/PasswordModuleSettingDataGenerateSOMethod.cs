using UnityEngine;

[CreateAssetMenu(
    fileName = "PasswordModuleSettingDataGenerateSOMethod",
    menuName = "Module/Setting/Password"
)]
public class PasswordModuleSettingDataGenerateSOMethod
    : TypeModuleSettingDataGenerateSOMethod<PasswordModuleSettingData>
{
    [SerializeField] private string password = "0000";

    protected override PasswordModuleSettingData GeneratePacketType()
    {
        return new PasswordModuleSettingData
        {
            password = password
        };
    }
}