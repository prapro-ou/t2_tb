using UnityEngine;

public class PasswordModuleToolsOrchestrator : MonoBehaviour
{
    [SerializeField] private PasswordModule passwordModule;

    public void InitializeModule(PasswordModuleSettingData settingData)
    {
        if (passwordModule != null)
        {
            passwordModule.SetPassword(settingData.password);
        }
    }
}