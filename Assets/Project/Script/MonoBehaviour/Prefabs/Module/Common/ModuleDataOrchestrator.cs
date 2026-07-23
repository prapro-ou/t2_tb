using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
public class ModuleDataOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleColorSettingOrchestrator _moduleColorSettingOrchestrator;
    [SerializeField] private ModuleSuccessOrchestrator _moduleSuccessOrchestrator;
    [SerializeField] private ModuleFailedOrchestrator _moduleFailedOrchestrator;
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