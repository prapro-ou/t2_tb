using UnityEngine;
using Cysharp.Threading.Tasks;
public class DialModuleMainOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleVersionEnum _moduleVersionEnum;
    public ModuleVersionEnum ModuleVersionEnum => _moduleVersionEnum;
    [SerializeField] private ModuleToolsOrchestratorBase _toolsOrchestratorBase;
    [SerializeField] private DialModuleAnimationMediator _animationMediator;
    [SerializeField] private DialModuleRotationMediator _rotationMediator;
    [SerializeField] private DialModuleCheckMediator _checkMediator;
    [SerializeField] private DialModuleMarkerMediator _markerMediator;
    [SerializeField] private DialModuleLockMediator _lockMediator;
    [SerializeField] private DialModuleSuccessMediator _successMediator;
    private int _markerCount = 12;
    public int MarkerCount => _markerCount;
    public void Initialize(DialModuleSettingData settingData)
    {
        AsyncInitialize(settingData).Forget();
    }

    private async UniTask AsyncInitialize(DialModuleSettingData settingData)
    {
        _rotationMediator.Initialize();
        _checkMediator.Initialize();
        _markerMediator.Initialize(settingData);
        await _animationMediator.InitializeAnimation();
        _lockMediator.Initialize(settingData);
        _successMediator.Initialize(settingData);
        _toolsOrchestratorBase.SetInitialize();
    }
}