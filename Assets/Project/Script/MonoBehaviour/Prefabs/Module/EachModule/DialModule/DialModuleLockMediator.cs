using System.Collections.Generic;
using UnityEngine;

public class DialModuleLockMediator : MonoBehaviour
{
    [SerializeField] private ModuleToolsOrchestratorIndividual<DialModuleSettingData> _moduleTools;
    [SerializeField] private ModuleDataOrchestrator _moduleData;
    [SerializeField] private DialModuleMainOrchestrator _dialModuleMain;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Transform _dialTransform;
    private DialModuleSettingData _dialModuleSettingData;
    private readonly float absArea = 17.5f;
    private int _lockAreaIndex = 0;
    private int _clearCount = 0;
    private bool _isLock = false;

    public void Initialize(DialModuleSettingData dialModuleSettingData)
    {
        _clearCount = 0;
        _dialModuleSettingData = dialModuleSettingData;
        LockAreaSet(_dialModuleMain.ModuleVersionEnum, _clearCount);
    }

    /// <summary>
    /// ロック配置
    /// </summary>
    /// <param name="moduleVersion"></param>
    /// <param name="index"></param>
    public void LockAreaSet(ModuleVersionEnum moduleVersion, int index)
    {
        // 0.初期確認
        if (!_dialModuleSettingData.MarkIndexList.TryGetValue(moduleVersion, out List<int> markerList))
        {
            Debug.Log("NonPairMarker");
            return;
        }
        _isLock = true;
        _lockAreaIndex = markerList[index];
    }

    public void Click()
    {
        if (_isLock)
        {
            _audioSource.Play();
            float absArg = Mathf.Abs(Mathf.DeltaAngle(_dialTransform.localEulerAngles.z, _lockAreaIndex * (360 / _dialModuleMain.MarkerCount)));
            if (absArg < absArea)
            {
                _clearCount++;
                _dialModuleSettingData.MarkIndexList.TryGetValue(_moduleData.ThisModuleSettingData.ModuleVersion, out List<int> markerList);
                _dialModuleSettingData.HaveMarkerModule.TryGetValue(_dialModuleMain.ModuleVersionEnum, out ModuleVersionEnum moduleVersion);
                _moduleTools.SendModuleInfoPacket(moduleVersion, new DialModuleMarkSetPacket()
                {
                    ModuleVersionEnum = _moduleData.ThisModuleSettingData.ModuleVersion,
                    Index = _clearCount
                });
                if (_clearCount >= markerList.Count)
                {
                    _isLock = false;
                    foreach (var item in _dialModuleSettingData.MarkIndexList.Keys)
                    {
                        _moduleTools.SendModuleInfoPacket(item, new DialModuleSuccessPacket()
                        {
                            ModuleVersionEnum = _moduleData.ThisModuleSettingData.ModuleVersion
                        });
                    }
                }
                else
                {
                    LockAreaSet(_dialModuleMain.ModuleVersionEnum, _clearCount);
                }
                return;
            }
            else
            {
                _moduleTools.ModuleFailed();
            }
        }
    }
}