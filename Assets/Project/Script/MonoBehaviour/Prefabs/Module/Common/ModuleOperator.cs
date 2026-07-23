using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
using Cysharp.Threading.Tasks;
public class ModuleOperator : MonoBehaviour
{
    [SerializeField] private ModuleColorSettingOrchestrator _moduleColorSettingOrchestrator;
    public ProductUserId HostUserId;
    public async UniTask Initialize(ProductUserId productUserId, Dictionary<ProductUserId, Color> color, ModuleSettingData moduleSettingData)
    {
        HostUserId = productUserId;
        await GetComponentInChildren<ModuleToolsOrchestratorBase>().Initialize(productUserId, moduleSettingData);
        _moduleColorSettingOrchestrator.Initialize(color);
    }
}