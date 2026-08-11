using System;
using UnityEngine;
using System.Collections.Generic;
using Epic.OnlineServices;
public struct GameSettingPacket : IPacketType
{
    public ProductUserId HostUserId;
    public List<ProductUserId> PlayerUserIds;
    public int ModuleCount;
    public TimeSpan GameLimitTime;
    public Dictionary<ProductUserId, Color> PlayerColors;
    public Dictionary<ProductUserId, List<ModuleSettingData>> ModuleSettingDatas;
}