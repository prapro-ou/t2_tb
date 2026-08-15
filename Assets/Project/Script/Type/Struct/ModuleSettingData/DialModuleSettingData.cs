using System.Collections.Generic;
using UnityEngine;
public struct DialModuleSettingData : IModuleSettingData
{
    public Dictionary<ModuleVersionEnum, List<int>> MarkIndexList { get; set; }
    public Dictionary<ModuleVersionEnum, ModuleVersionEnum> HaveMarkerModule { get; set; }
}