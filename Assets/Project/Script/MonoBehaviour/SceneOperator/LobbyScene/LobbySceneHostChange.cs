using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using OriginalNameSpace.EOSMethod.Lobby;
public class LobbySceneHostChange : MonoBehaviour
{
    [SerializeField] private GameObject _hostPanel;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    private ulong _notificationId = 0;

    public void Initialize()
    {
        var hostID = EOSLobbyMethod.GetLobbyHostPuid(_eosLobbyOperator.CurrentLobbyId);
        ChangeHost(hostID);
        _notificationId = EOSLobbyMethod.RegisterLobbyNotifications(_eosLobbyOperator.CurrentLobbyId, HostChange);
    }

    public void HostChange(LobbyMemberStatusReceivedCallbackInfo info)
    {
        if (info.CurrentStatus != LobbyMemberStatus.Promoted) return;
        ChangeHost(info.TargetUserId);
    }

    private void ChangeHost(ProductUserId targetUserID)
    {
        if (targetUserID == _eosLobbyOperator.LocalProductUserId)
        {
            _hostPanel.SetActive(true);
        }
        else
        {
            _hostPanel.SetActive(false);
        }
    }

    public void OnDestroy()
    {
        EOSLobbyMethod.UnregisterLobbyNotifications(_notificationId);
    }
}