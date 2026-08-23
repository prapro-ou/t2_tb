using System.Collections.Generic;

public struct SymbolModuleSettingData : IModuleSettingData
{
    public List<int> SymbolIndices { get; set; }
    public List<int> CorrectOrder { get; set; }
}
