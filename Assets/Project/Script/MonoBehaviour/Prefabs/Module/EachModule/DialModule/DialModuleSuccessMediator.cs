using System.Collections.Generic;
using UnityEngine;
public class DialModuleSuccessMediator : MonoBehaviour
{
    [SerializeField] private ModuleToolsOrchestratorIndividual<DialModuleSettingData> _moduleTools;
    private string _uuid;
    private Dictionary<ModuleVersionEnum, bool> _successModule;
    public void Initialize(DialModuleSettingData dialModuleSettingData)
    {
        _successModule = new Dictionary<ModuleVersionEnum, bool>();
        foreach (var item in dialModuleSettingData.MarkIndexList.Keys)
        {
            _successModule.Add(item, false);
        }
        _uuid = _moduleTools.ModuleDataRegisterListener<DialModuleSuccessPacket>(OnSuccess);
    }

    private void OnSuccess(ModuleVersionEnum moduleVersionEnum, DialModuleSuccessPacket dialModuleMarkSetPacket)
    {
        _successModule[dialModuleMarkSetPacket.ModuleVersionEnum] = true;
        foreach (var item in _successModule.Values)
        {
            if (item == false) return;
        }
        _moduleTools.ModuleSuccess();
    }
    private void OnDestroy()
    {
        _moduleTools.ModuleDataUnregisterListener(_uuid);
    }
}