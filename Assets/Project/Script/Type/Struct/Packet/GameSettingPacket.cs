using UnityEngine;
using System.Collections.Generic;
using Epic.OnlineServices;
public struct GameSettingPacket : IPacketType
{
    public ProductUserId HostUserId;
    public Dictionary<ProductUserId, Color> PlayerColors;
    public Dictionary<ProductUserId, List<ModuleSettingData>> ModuleSettingDatas;
}