using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class ModuleFailedOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private List<UnityEvent> _events;
    public void Initialize()
    {
        EOSP2PMethod.RegisterListener<ModuleFailedPacket>(OnFailedPacketReceived);
    }

    private void OnFailedPacketReceived(ProductUserId remoteUserId, string socketName, ModuleFailedPacket packet)
    {
        if (packet.moduleType != _moduleDataOrchestrator.ThisModuleSettingData.ModuleType) return;
        _events.ForEach(x => x.Invoke());
        OnFailed();
    }
    private void OnFailed()
    {

    }
}