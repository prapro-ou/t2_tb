using System.Collections.Generic;
using Epic.OnlineServices;
public struct ModuleSettingData
{
    public ModuleTypeEnum ModuleType { get; set; }
    public ModuleVersionEnum ModuleVersion { get; set; }
    public Dictionary<ModuleVersionEnum, ProductUserId> ModuleGroup { get; set; }
    public IModuleSettingData ModuleData { get; set; }
}