using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class ModuleSuccessOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private List<UnityEvent> _events;
    private string _uuid;
    public void Initialize()
    {
        _uuid = EOSP2PMethod.RegisterListener<ModuleSuccessPacket>(OnSuccessPacketReceived);
    }

    private void OnSuccessPacketReceived(ProductUserId remoteUserId, string socketName, ModuleSuccessPacket packet)
    {
        if (packet.ModuleNumber != _moduleDataOrchestrator.ThisModuleSettingData.ModuleNumber) return;
        _events.ForEach(x => x.Invoke());
        OnSuccess();
    }

    private void OnSuccess()
    {
        _gameObject.SetActive(true);
        _moduleDataOrchestrator.SetSuccess();
    }

    private void OnDestroy()
    {
        EOSP2PMethod.UnregisterListener(_uuid);
    }
}