using UnityEngine;
using OriginalNameSpace.EOSMethod.Lobby;
using OriginalNameSpace.EOSMethod.P2P;
using Cysharp.Threading.Tasks;
public class CoreSceneEOSOrchestrator : MonoBehaviour
{
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    public void Initialize()
    {
        EOSLobbyMethod.LoginAsync().Forget();
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