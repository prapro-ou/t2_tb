using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
public class ModuleDataOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleColorSettingOrchestrator _colorSettingOrchestrator;
    public ModuleColorSettingOrchestrator ColorSettingOrchestrator => _colorSettingOrchestrator;
    [SerializeField] private ModuleSuccessOrchestrator _successOrchestrator;
    public ModuleSuccessOrchestrator SuccessOrchestrator => _successOrchestrator;
    [SerializeField] private ModuleFailedOrchestrator _failedOrchestrator;
    public ModuleFailedOrchestrator FailedOrchestrator => _failedOrchestrator;

    public ProductUserId HostId { get; private set; }
    public Dictionary<ProductUserId, Color> UserColors { get; private set; }
    public ModuleSettingData ThisModuleSettingData { get; private set; }
    public void Initialize(ProductUserId hostId, Dictionary<ProductUserId, Color> userColor, ModuleSettingData moduleSettingData)
    {
        HostId = hostId;
        UserColors = userColor;
        ThisModuleSettingData = moduleSettingData;
    }
}