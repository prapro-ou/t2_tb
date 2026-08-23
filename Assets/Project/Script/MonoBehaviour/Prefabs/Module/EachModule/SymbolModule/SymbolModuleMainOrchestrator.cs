using UnityEngine;

public class SymbolModuleMainOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleToolsOrchestratorBase _toolsOrchestratorBase;
    [SerializeField] private SymbolManager _symbolManager;

    public void Initialize(SymbolModuleSettingData settingData)
    {
        Debug.Log("SymbolModule Initialize");

        Debug.Log("Symbols: " + string.Join(", ", settingData.SymbolIndices));
        Debug.Log("CorrectOrder: " + string.Join(", ", settingData.CorrectOrder));

        _symbolManager.Initialize(settingData);

        _toolsOrchestratorBase.SetInitialize();
    }
}
