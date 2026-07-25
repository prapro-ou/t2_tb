using System.Collections.Generic;

public abstract class TypeModuleSettingDataGenerateSOMethod<T> : ModuleSettingDataGenerateSOMethod where T : IModuleSettingData
{
    public override sealed IModuleSettingData Generate()
    {
        return GeneratePacketType();
    }

    /// <summary>
    /// 生成するIPacketType
    /// </summary>
    /// <returns>T</returns>
    protected abstract T GeneratePacketType();
}