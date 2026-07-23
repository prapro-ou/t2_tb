using System;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;

public static class ModuleCommonMethod
{
    /// <summary>
    /// モジュールデータ受信
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="remoteUserId"></param>
    /// <param name="socketName"></param>
    /// <param name="packet"></param>
    public static void ModuleDataRegisterListener<T>(Action<ProductUserId, string, T> onPacketReceived) where T : IPacketType
    {
        EOSP2PMethod.RegisterListener<T>(onPacketReceived);
    }
}
