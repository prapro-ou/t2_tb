using TMPro;
using UnityEngine;

public class PasswordInstructionModule : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;

    public void InitializeModule(PasswordModuleSettingData settingData)
    {
        SetPassword(settingData.password);
    }
    public void SetPassword(string password)
    {
        displayText.text = password;
    }
}