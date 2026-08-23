using UnityEngine;

public class SymbolModuleMainOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleToolsOrchestratorBase _toolsOrchestratorBase;
    [SerializeField] private SymbolManager _symbolManager;
    [SerializeField] private SymbolManualManager _symbolManualManager;

    public void Initialize(SymbolModuleSettingData settingData)
    {
        Debug.Log("SymbolModule Initialize");

        // 解除側
        _symbolManager.Initialize(settingData);

        // 指示側
        _symbolManualManager.Initialize(settingData);

        _toolsOrchestratorBase.SetInitialize();
    }
}
