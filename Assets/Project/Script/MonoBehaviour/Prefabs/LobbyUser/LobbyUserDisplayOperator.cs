using UnityEngine;

public class LobbyUserDisplayOperator : MonoBehaviour
{
    [SerializeField] private LobbyUserDisplaySettingOrchestrator _settingOrchestrator = null;

    public void InitializeLobbyUserDisplay(string userName, bool host)
    {
        _settingOrchestrator.SetLobbyUserDisplaySetting(userName, host);
    }
}
