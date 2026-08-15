using System;

// 1. IModuleSettingDataを継承したstruct
[Serializable]
public struct ShapePuzzleModuleSettingData : IModuleSettingData
{
    public int[] correctOrder; // 正解の配置
    public int[] initialOrder; // 初期配置
}