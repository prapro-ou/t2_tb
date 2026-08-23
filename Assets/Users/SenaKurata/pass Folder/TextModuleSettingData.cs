using UnityEngine;

[System.Serializable]
public struct TextModuleSettingData : IModuleSettingData
{
    // モジュールに表示させる正解の文字列（例: "A3B7", "3914" など）
    public string targetText;
}