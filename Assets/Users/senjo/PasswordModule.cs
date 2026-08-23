using TMPro;
using UnityEngine;

public class PasswordModule : MonoBehaviour
{
    [SerializeField] private PasswordModuleToolsOrchestrator _passwordModuleToolsOrchestrator;
    [SerializeField] private GameSettingActiveSOData _gameSettingActiveSOData;
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;
    [SerializeField] private Canvas _canvas;
    public TMP_Text displayText;
    private string correctcode;

    public void InitializeModule(PasswordModuleSettingData settingData)
    {
        SetPassword(settingData.password);
    }
    public void SetPassword(string password)
    {
        _canvas.worldCamera = _gameStatusActiveSOData.Camera;
        correctcode = password;
    }
    public TMP_Text resultText;

    private string input = "";

    public int maxLength = 4;

    void Start()
    {
        UpdateDisplay();
    }

    public void PressNumber(int number)
    {
        Debug.Log("Pressed number: " + number);
        resultText.text = "";

        if (input.Length >= maxLength)
            return;

        input += number.ToString();
        UpdateDisplay();
    }

    public void ClearInput()
    {
        input = "";
        UpdateDisplay();
    }

    public void Enter()
    {
        if (input == correctcode)
        {
            resultText.text = "ACCESS GRANTED";
            _passwordModuleToolsOrchestrator.ModuleSuccess();
        }
        else
        {
            resultText.text = "ACCESS DENIED";
            _passwordModuleToolsOrchestrator.ModuleFailed();
            ClearInput();
        }
    }

    void UpdateDisplay()
    {
        displayText.text = input.PadRight(maxLength, '_');
    }
}