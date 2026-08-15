using System;
using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;

public class StopwatchModuleToolsOrchestrator
    : ModuleToolsOrchestratorBase
{
    // =========================================================
    // Property
    // =========================================================

    public ModuleSettingData ThisModuleSettingData { get; private set; }

    private ProductUserId _hostUserId;

    private List<string> listenerList = new List<string>();


    // =========================================================
    // Initialize
    // =========================================================

    /// <summary>
    /// ストップウォッチモジュールの初期化
    /// </summary>
    public override void Initialize(
        ProductUserId productUserId,
        ModuleSettingData moduleSettingData)
    {
        // 既存リスナーを解除
        foreach (string uuid in listenerList)
        {
            EOSP2PMethod.UnregisterListener(uuid);
        }

        listenerList.Clear();

        // 外部設定を保存
        _hostUserId = productUserId;
        ThisModuleSettingData = moduleSettingData;

        Debug.Log("StopwatchModuleToolsOrchestrator initialized.");

        SetInitialize();
    }


    // =========================================================
    // ModuleInfo
    // =========================================================

    /// <summary>
    /// モジュール情報を送信する
    /// </summary>
    public void SendModuleInfoPacket<PacketT>(
        ModuleVersionEnum moduleVersion,
        PacketT packet)
        where PacketT : IPacketType
    {
        EOSP2PMethod.SendPacket(
            SocketNameEnum.ModuleInfo,
            ThisModuleSettingData.ModuleGroup[moduleVersion],
            packet
        );
    }


    /// <summary>
    /// モジュール情報の受信登録
    /// </summary>
    public string ModuleDataRegisterListener<PacketT>(
        Action<ModuleVersionEnum, PacketT> onPacketReceived)
        where PacketT : IPacketType
    {
        string uuid =
            EOSP2PMethod.RegisterListener<PacketT>(
                (remoteUserId, socketName, payload) =>
                {
                    if (socketName !=
                        SocketNameEnum.ModuleInfo.ToString())
                    {
                        return;
                    }

                    ModuleVersionEnum targetKey =
                        new ModuleVersionEnum();

                    foreach (
                        var pair
                        in ThisModuleSettingData.ModuleGroup)
                    {
                        if (pair.Value == remoteUserId)
                        {
                            targetKey = pair.Key;
                            break;
                        }
                    }

                    onPacketReceived(targetKey, payload);
                });

        listenerList.Add(uuid);

        return uuid;
    }


    /// <summary>
    /// モジュール情報の受信登録を解除
    /// </summary>
    public void ModuleDataUnregisterListener(string uuid)
    {
        listenerList.Remove(uuid);

        EOSP2PMethod.UnregisterListener(uuid);
    }


    // =========================================================
    // START / STOP
    // =========================================================

    /// <summary>
    /// START / STOPを送信する
    /// </summary>
    public void SendAction(
        ModuleVersionEnum moduleVersion,
        bool isStart)
    {
        SendModuleInfoPacket(
            moduleVersion,
            new StopwatchActionPacket
            {
                IsStart = isStart
            }
        );
    }


    /// <summary>
    /// START / STOPを受信したときの処理を登録する
    /// </summary>
    public string RegisterActionListener(
        Action<ModuleVersionEnum, bool> onActionReceived)
    {
        return ModuleDataRegisterListener<StopwatchActionPacket>(
            (moduleVersion, packet) =>
            {
                onActionReceived(
                    moduleVersion,
                    packet.IsStart
                );
            }
        );
    }


    // =========================================================
    // Time
    // =========================================================

    /// <summary>
    /// 計測時間を送信する
    /// </summary>
    public void SendTime(
        ModuleVersionEnum moduleVersion,
        float timeCount)
    {
        SendModuleInfoPacket(
            moduleVersion,
            new StopwatchTimePacket
            {
                TimeCount = timeCount
            }
        );
    }


    /// <summary>
    /// 計測時間を受信したときの処理を登録する
    /// </summary>
    public string RegisterTimeListener(
        Action<ModuleVersionEnum, float> onTimeReceived)
    {
        return ModuleDataRegisterListener<StopwatchTimePacket>(
            (moduleVersion, packet) =>
            {
                onTimeReceived(
                    moduleVersion,
                    packet.TimeCount
                );
            }
        );
    }


    // =========================================================
    // Module Version
    // =========================================================

    /// <summary>
    /// 自分のモジュールバージョンを取得する
    /// </summary>
    public ModuleVersionEnum GetMyModuleVersion()
    {
        return ThisModuleSettingData.ModuleVersion;
    }


    // =========================================================
    // Success / Failed
    // =========================================================

    /// <summary>
    /// モジュール解除成功をホストに通知する
    /// </summary>
    public void ModuleSuccess()
    {
        EOSP2PMethod.SendPacket(
            SocketNameEnum.ModuleInfo,
            _hostUserId,
            new ModuleSuccessPacket
            {
                moduleType =
                    ThisModuleSettingData.ModuleType
            }
        );
    }


    /// <summary>
    /// モジュール解除失敗をホストに通知する
    /// </summary>
    public void ModuleFailed()
    {
        EOSP2PMethod.SendPacket(
            SocketNameEnum.ModuleInfo,
            _hostUserId,
            new ModuleFailedPacket
            {
                moduleType =
                    ThisModuleSettingData.ModuleType
            }
        );
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        foreach (string uuid in listenerList)
        {
            EOSP2PMethod.UnregisterListener(uuid);
        }

        listenerList.Clear();
    }
}