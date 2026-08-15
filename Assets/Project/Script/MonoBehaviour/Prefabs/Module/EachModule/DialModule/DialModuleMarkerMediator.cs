using System.Collections.Generic;
using UnityEngine;
public class DialModuleMarkerMediator : MonoBehaviour
{
    [SerializeField] private ModuleToolsOrchestratorIndividual<DialModuleSettingData> _moduleTools;
    [SerializeField] private ModuleDataOrchestrator _moduleData;
    [SerializeField] private DialModuleMainOrchestrator _dialModuleMain;
    [SerializeField] private GameObject _markerObject;
    [SerializeField] private SpriteRenderer _markerRenderer;
    [SerializeField] private Transform _markerTransform;
    private DialModuleSettingData _dialModuleSettingData;
    private string _uuid;
    public void Initialize(DialModuleSettingData dialModuleSettingData)
    {
        _dialModuleSettingData = dialModuleSettingData;
        if (!_dialModuleSettingData.HaveMarkerModule.TryGetValue(_dialModuleMain.ModuleVersionEnum, out ModuleVersionEnum moduleVersion))
        {
            Debug.Log("NonMarkerModule");
            return;
        }
        _uuid = _moduleTools.ModuleDataRegisterListener<DialModuleMarkSetPacket>(OnReceiveMarkPacket);
        MarkerSet(moduleVersion, 0);
    }

    public void MarkerSet(ModuleVersionEnum moduleVersion, int index)
    {
        // 0.初期確認
        if (!_dialModuleSettingData.MarkIndexList.TryGetValue(moduleVersion, out List<int> pairMarkerList))
        {
            Debug.Log("NonPairMarker");
            return;
        }
        if (!_moduleData.ThisModuleSettingData.ModuleGroup.TryGetValue(moduleVersion, out var userId))
        {
            return;
        }
        // 1.マーカー配置
        _markerRenderer.color = _moduleData.UserColors.TryGetValue(userId, out Color userColor) ? userColor : Color.white;
        if (index >= pairMarkerList.Count)
        {
            _markerObject.SetActive(false);
            return;
        }
        _markerTransform.rotation = Quaternion.Euler(new Vector3(0, 0, pairMarkerList[index] * (360 / _dialModuleMain.MarkerCount)));//配置
        _markerObject.SetActive(true);//表示
    }

    private void OnReceiveMarkPacket(ModuleVersionEnum moduleVersionEnum, DialModuleMarkSetPacket dialModuleMarkSetPacket)
    {
        if (dialModuleMarkSetPacket.ModuleVersionEnum != _dialModuleSettingData.HaveMarkerModule[_dialModuleMain.ModuleVersionEnum]) return;
        MarkerSet(dialModuleMarkSetPacket.ModuleVersionEnum, dialModuleMarkSetPacket.Index);
    }

    private void OnDestroy()
    {
        _moduleTools.ModuleDataUnregisterListener(_uuid);
    }

}