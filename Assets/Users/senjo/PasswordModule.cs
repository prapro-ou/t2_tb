using TMPro;
using UnityEngine;

public class PasswordModule : MonoBehaviour
{
    public TMP_Text displayText;
    private string correctcode;

    public void SetPassword(string password)
    {
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
        if(input == correctcode)
        {
            resultText.text = "EXIT";
        }
        else
        {
            resultText.text = "ERROR";
            ClearInput();
        }
    }

    void UpdateDisplay()
    {
        displayText.text = input.PadRight(maxLength, '_');
    }
}