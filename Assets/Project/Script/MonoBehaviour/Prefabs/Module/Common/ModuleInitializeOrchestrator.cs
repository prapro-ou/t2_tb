using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
public class ModuleInitializeOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private ModuleColorOrchestrator _moduleColorSettingOrchestrator;
    [SerializeField] private ModuleSuccessOrchestrator _moduleSuccessOrchestrator;
    [SerializeField] private ModuleFailedOrchestrator _moduleFailedOrchestrator;
    [SerializeField] private GameObject _moduleToolsOrchestratorObject;
    public async UniTask Initialize(ProductUserId hostId, Dictionary<ProductUserId, Color> color, ModuleSettingData moduleSettingData)
    {
        ModuleToolsOrchestratorBase moduleToolsOrchestrator = _moduleToolsOrchestratorObject.GetComponentInChildren<ModuleToolsOrchestratorBase>();
        _moduleDataOrchestrator.Initialize(hostId, color, moduleSettingData);
        moduleToolsOrchestrator.Initialize(hostId, moduleSettingData);
        _moduleSuccessOrchestrator.Initialize();
        _moduleFailedOrchestrator.Initialize();
        await _moduleColorSettingOrchestrator.Initialize();

        await UniTask.WaitUntil(() => moduleToolsOrchestrator.IsInitialize);
    }
}