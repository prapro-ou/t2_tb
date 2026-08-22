using UnityEngine;
using OriginalNameSpace.EOSMethod.Lobby;
using OriginalNameSpace.EOSMethod.P2P;
using Cysharp.Threading.Tasks;
public class CoreSceneEOSOrchestrator : MonoBehaviour
{
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    public async UniTask Initialize()
    {
        while (_eosLobbyOperator.LocalProductUserId == null)
        {
            await _eosLobbyOperator.InitializeAndLoginAsync();
        }

        EOSP2PMethod.StartListening(SocketNameEnum.Fallback);
    }

    public void Update()
    {
        EOSP2PMethod.UpdateReceiveLoop();
    }
    public void OnDestroy()
    {
        EOSP2PMethod.StopAllListening();
        EOSP2PMethod.UnregisterAllListeners();
        EOSLobbyMethod.UnregisterAllNotifications();
        EOSLobbyMethod.LeaveLobbyAsync(_eosLobbyOperator.CurrentLobbyId).Forget();
        EOSLobbyMethod.LogoutAsync().Forget();
    }
}