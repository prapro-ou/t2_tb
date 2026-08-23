using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using OriginalNameSpace.EOSMethod.Lobby;
public class LobbySceneHostChange : MonoBehaviour
{
    [SerializeField] private GameObject _hostPanel;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private AudioSource _audioSource;
    private ulong _notificationId = 0;

    public void Initialize()
    {
        var hostID = EOSLobbyMethod.GetLobbyHostPuid(_eosLobbyOperator.CurrentLobbyId);
        ChangeHostDisplay(hostID).Forget();
        _notificationId = EOSLobbyMethod.RegisterLobbyNotifications(_eosLobbyOperator.CurrentLobbyId, ChangeHost);
    }

    public void ChangeHost(LobbyMemberStatusReceivedCallbackInfo info)
    {
        if (info.CurrentStatus != LobbyMemberStatus.Promoted) return;
        ChangeHostDisplay(info.TargetUserId).Forget();
    }

    private async UniTask ChangeHostDisplay(ProductUserId targetUserID)
    {
        await UniTask.WaitForSeconds(1.0f);
        _audioSource.Play();
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