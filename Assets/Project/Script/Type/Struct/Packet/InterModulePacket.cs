public struct InterModulePacket : IPacketType
{
    public int ModuleNumber;
    public IPacketType Data;
}