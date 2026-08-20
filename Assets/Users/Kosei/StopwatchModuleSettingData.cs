using System;

[Serializable]
public struct StopwatchModuleSettingData : IModuleSettingData
{
    public float minTargetTime;
    public float maxTargetTime;
    public float tolerance;
    public int questionCount;
}