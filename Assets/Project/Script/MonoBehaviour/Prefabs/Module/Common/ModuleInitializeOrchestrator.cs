using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
public class ModuleInitializeOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private ModuleColorSettingOrchestrator _moduleColorSettingOrchestrator;
    [SerializeField] private ModuleSuccessOrchestrator _moduleSuccessOrchestrator;
    [SerializeField] private ModuleFailedOrchestrator _moduleFailedOrchestrator;

    [SerializeField] private GameObject _moduleToolsOrchestratorObject;
    public void Initialize(ProductUserId hostId, Dictionary<ProductUserId, Color> color, ModuleSettingData moduleSettingData)
    {
        _moduleDataOrchestrator.Initialize(hostId, color, moduleSettingData);
        _moduleToolsOrchestratorObject.GetComponentInChildren<ModuleToolsOrchestratorBase>().Initialize(hostId, moduleSettingData);
        _moduleColorSettingOrchestrator.Initialize();
        _moduleSuccessOrchestrator.Initialize();
        _moduleFailedOrchestrator.Initialize();
    }
}