using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class LobbyUserDisplaySettingOrchestrator : MonoBehaviour
{
    [SerializeField] private Image _hostImage;
    [SerializeField] private TMP_Text _userNameText;
    public void SetUserName(string userName)
    {
        _userNameText.text = userName;
    }

    public void SetHostImage(bool isHost)
    {
        _hostImage.enabled = isHost;
    }
}
