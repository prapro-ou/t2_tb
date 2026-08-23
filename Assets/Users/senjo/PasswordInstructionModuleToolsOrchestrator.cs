using UnityEngine;

public class PasswordInstructionModuleToolsOrchestrator
    : ModuleToolsOrchestratorIndividual<PasswordModuleSettingData>
{
    [SerializeField] private PasswordInstructionModule passwordInstructionModule;

    public void InitializeModule(PasswordModuleSettingData settingData)
    {
        if (passwordInstructionModule != null)
        {
            passwordInstructionModule.SetPassword(settingData.password);
        }
    }
}