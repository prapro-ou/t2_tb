using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public abstract class ModuleToolsOrchestratorIndividual<T> : ModuleToolsOrchestratorBase where T : IModuleSettingData
{
    #region  ========== Property ==========
    public ModuleSettingData ThisModuleSettingData { get; private set; }
    [SerializeField] private UnityEvent<T> _onInitialize = new UnityEvent<T>();
    private ProductUserId _hostUserId;
    private List<string> listenerList = new List<string>();
    #endregion ========== Property ==========

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="moduleSettingData"></param>
    /// <returns></returns>
    public override sealed void Initialize(ProductUserId productUserId, ModuleSettingData moduleSettingData)
    {
        // 0.初期確認

        // 1.内部設定初期化
        listenerList.Clear();

        // 2.外部設定初期化
        _hostUserId = productUserId;
        ThisModuleSettingData = moduleSettingData;
        if (moduleSettingData.ModuleData is T data)
        {
            _onInitialize.Invoke(data);
        }
        else
        {
            Debug.LogError("ModuleData is not T");
        }
    }

    #region ========== public Tools Method ==========

    #region  ========== ModuleInfo ==========

    /// <summary>
    /// モジュール情報送信
    /// </summary>
    /// <typeparam name="PacketT"></typeparam>
    /// <param name="moduleVersion"></param>
    /// <param name="packet"></param>
    /// <returns></returns>
    public void SendModuleInfoPacket<PacketT>(ModuleVersionEnum moduleVersion, PacketT packet) where PacketT : IPacketType
    {
        EOSP2PMethod.SendPacket(SocketNameEnum.ModuleInfo, ThisModuleSettingData.ModuleGroup[moduleVersion], packet);
    }

    /// <summary>
    /// モジュール情報受信登録
    /// </summary>
    /// <typeparam name="PacketT"></typeparam>
    /// <param name="onPacketReceived"></param>
    public string ModuleDataRegisterListener<PacketT>(Action<ModuleVersionEnum, PacketT> onPacketReceived) where PacketT : IPacketType
    {
        string uuid = EOSP2PMethod.RegisterListener<PacketT>((remoteUserId, socketName, payload) =>
        {
            if (socketName != SocketNameEnum.ModuleInfo.ToString()) return;
            ModuleVersionEnum targetKey = new ModuleVersionEnum();
            foreach (var pair in ThisModuleSettingData.ModuleGroup)
            {
                if (pair.Value == remoteUserId)
                {
                    targetKey = pair.Key;
                    break; // 最初に見つかった時点で抜ける
                }
            }
            onPacketReceived(targetKey, payload);
        });
        listenerList.Add(uuid);
        return uuid;
    }

    /// <summary>
    /// モジュール情報受信解除
    /// </summary>
    public void ModuleDataUnregisterListener(string uuid)
    {
        listenerList.Remove(uuid);
        EOSP2PMethod.UnregisterListener(uuid);
    }

    /// <summary>
    /// 受信解除処理
    /// </summary>
    private void OnDisable()
    {
        foreach (string uuid in listenerList)
        {
            EOSP2PMethod.UnregisterListener(uuid);
        }
    }

    #endregion ========== ModuleInfo ==========

    #region  ========== GameInfo ==========

    /// <summary>
    /// モジュール解除成功
    /// </summary>
    public void ModuleSuccess()
    {
        EOSP2PMethod.SendPacket(SocketNameEnum.ModuleInfo, _hostUserId, new ModuleSuccessPacket());
    }

    /// <summary>
    /// モジュール解除失敗
    /// </summary>
    public void ModuleFailed()
    {
        EOSP2PMethod.SendPacket(SocketNameEnum.ModuleInfo, _hostUserId, new ModuleFailedPacket());
    }

    #endregion ========== GameInfo ==========

    #endregion ========== public Tools Method ==========

}