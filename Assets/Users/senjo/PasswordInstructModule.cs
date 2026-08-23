using TMPro;
using UnityEngine;

public class PasswordInstructionModule : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;

    public void SetPassword(string password)
    {
        displayText.text = password;
    }
}