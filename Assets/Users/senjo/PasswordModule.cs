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
        SetPassword("0000");
        UpdateDisplay();
    }

    public void PressNumber(int number)
    {
        Debug.Log("click: " + number);

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
        if(input == correctcode)
        {
            resultText.text = "EXIT";
            ClearInput();
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