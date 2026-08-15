using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
public class ModuleDataOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleColorOrchestrator _colorSettingOrchestrator;
    public ModuleColorOrchestrator ColorSettingOrchestrator => _colorSettingOrchestrator;
    [SerializeField] private ModuleSuccessOrchestrator _successOrchestrator;
    public ModuleSuccessOrchestrator SuccessOrchestrator => _successOrchestrator;
    [SerializeField] private ModuleFailedOrchestrator _failedOrchestrator;
    public ModuleFailedOrchestrator FailedOrchestrator => _failedOrchestrator;
    public ProductUserId HostId { get; private set; }
    public List<ProductUserId> Players { get; private set; }
    public Dictionary<ProductUserId, Color> UserColors { get; private set; }
    public ModuleSettingData ThisModuleSettingData { get; private set; }
    public bool IsSuccess { get; private set; }
    public void Initialize(ProductUserId hostId, List<ProductUserId> players, Dictionary<ProductUserId, Color> userColor, ModuleSettingData moduleSettingData)
    {
        IsSuccess = false;
        HostId = hostId;
        Players = players;
        UserColors = userColor;
        ThisModuleSettingData = moduleSettingData;
    }
    public void SetSuccess() => IsSuccess = true;
}