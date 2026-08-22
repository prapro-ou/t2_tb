using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
public class ModuleInitializeOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private ModuleColorOrchestrator _moduleColorSettingOrchestrator;
    [SerializeField] private ModuleSuccessOrchestrator _moduleSuccessOrchestrator;
    [SerializeField] private ModuleFailedOrchestrator _moduleFailedOrchestrator;
    [SerializeField] private GameObject _moduleToolsOrchestratorObject;
    [SerializeField] private TMP_Text _moduleNumberText;
    public async UniTask Initialize(ProductUserId hostId, List<ProductUserId> players, Dictionary<ProductUserId, Color> color, ModuleSettingData moduleSettingData)
    {
        _moduleDataOrchestrator.Initialize(hostId, players, color, moduleSettingData);
        ModuleToolsOrchestratorBase moduleToolsOrchestrator = _moduleToolsOrchestratorObject.GetComponentInChildren<ModuleToolsOrchestratorBase>();
        moduleToolsOrchestrator.Initialize(hostId, players, moduleSettingData);
        _moduleSuccessOrchestrator.Initialize();
        _moduleFailedOrchestrator.Initialize();
        if (moduleSettingData.ModuleNumber != -1)
        {
            _moduleNumberText.text = moduleSettingData.ModuleNumber.ToString();
        }
        UniTask task = _moduleColorSettingOrchestrator.Initialize();
        await task;
        await UniTask.WaitUntil(() => moduleToolsOrchestrator.IsInitialize);
    }
}