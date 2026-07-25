using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class ModuleSuccessOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private ModuleColorOrchestrator _moduleColorSettingOrchestrator;
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private List<UnityEvent> _events;

    public void Initialize()
    {
        EOSP2PMethod.RegisterListener<ModuleSuccessPacket>(OnSuccessPacketReceived);
    }

    private void OnSuccessPacketReceived(ProductUserId remoteUserId, string socketName, ModuleSuccessPacket packet)
    {
        if (packet.moduleType != _moduleDataOrchestrator.ThisModuleSettingData.ModuleType) return;
        _events.ForEach(x => x.Invoke());
        OnSuccess();
    }

    private void OnSuccess()
    {
        _gameObject.SetActive(true);
        _moduleColorSettingOrchestrator.SuccessColor();
    }
}