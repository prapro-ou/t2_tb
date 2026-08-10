using UnityEngine;
using Epic.OnlineServices;
public class LobbyUserDisplayOperator : MonoBehaviour
{
    [SerializeField] private LobbyUserDisplaySettingOrchestrator _settingOrchestrator = null;
    public ProductUserId UserId;
    public bool IsHost = false;
    public void InitializeLobbyUserDisplay(ProductUserId productUserId, string userName, bool isHost)
    {
        UserId = productUserId;
        isHost = IsHost;
        _settingOrchestrator.SetUserName(userName);
        _settingOrchestrator.SetHostImage(isHost);
    }

    public void HostChange(bool isHost)
    {
        IsHost = isHost;
        _settingOrchestrator.SetHostImage(isHost);
    }
}
