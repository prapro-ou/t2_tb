public enum StopwatchActionEnum
{
    Start,
    Stop
}

public struct StopwatchModuleActionPacket : IPacketType
{
    public StopwatchActionEnum Action;
}