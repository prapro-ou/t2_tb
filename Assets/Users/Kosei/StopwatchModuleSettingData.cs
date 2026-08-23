using UnityEngine;

[System.Serializable]
public struct StopwatchModuleSettingData : IModuleSettingData
{
    [Header("目標時間の設定")]
    public float minTargetTime;
    public float maxTargetTime;
    public float tolerance;
    public int questionCount;
}