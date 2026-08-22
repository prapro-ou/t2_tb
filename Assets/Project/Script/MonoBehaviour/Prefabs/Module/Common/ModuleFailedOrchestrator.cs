using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class ModuleFailedOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private List<UnityEvent> _events;
    private string _uuid;
    public void Initialize()
    {
        _uuid = EOSP2PMethod.RegisterListener<ModuleFailedPacket>(OnFailedPacketReceived);
    }

    private void OnFailedPacketReceived(ProductUserId remoteUserId, string socketName, ModuleFailedPacket packet)
    {
        if (packet.ModuleNumber != _moduleDataOrchestrator.ThisModuleSettingData.ModuleNumber) return;
        _events.ForEach(x => x.Invoke());
        OnFailed();
    }

    private void OnFailed()
    {

    }

    private void OnDestroy()
    {
        EOSP2PMethod.UnregisterListener(_uuid);
    }
}