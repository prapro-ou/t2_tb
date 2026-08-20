using System;

public enum StopwatchActionType
{
    Start,
    Stop
}

[Serializable]
public struct StopwatchActionPacket : IPacketType
{
    public StopwatchActionType ActionType;
}