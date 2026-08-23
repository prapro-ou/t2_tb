using UnityEngine;

public class SymbolModuleMainOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleToolsOrchestratorBase _toolsOrchestratorBase;
    [SerializeField] private SymbolManager _symbolManager;
    [SerializeField] private SymbolManualManager _symbolManualManager;

    public void Initialize(SymbolModuleSettingData settingData)
    {
        Debug.Log("SymbolModule Initialize");

        // A側（解除側）
        if (_symbolManager != null)
        {
            _symbolManager.Initialize(settingData);
        }

        // B側（指示側）
        if (_symbolManualManager != null)
        {
            _symbolManualManager.Initialize(settingData);
        }

        _toolsOrchestratorBase.SetInitialize();
    }
}