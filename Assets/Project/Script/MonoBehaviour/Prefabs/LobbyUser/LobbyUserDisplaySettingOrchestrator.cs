using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class LobbyUserDisplaySettingOrchestrator : MonoBehaviour
{
    [SerializeField] private Image _hostImage;
    [SerializeField] private TMP_Text _userNameText;
    public void SetLobbyUserDisplaySetting(string userName, bool host)
    {
        Debug.Log("LobbyUserDisplaySettingOrchestrator.SetLobbyUserDisplaySetting");
        Debug.Log(userName);
        _userNameText.text = userName;
        _hostImage.enabled = host;
    }
}
