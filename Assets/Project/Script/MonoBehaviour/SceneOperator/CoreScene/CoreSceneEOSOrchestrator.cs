using UnityEngine;
using OriginalNameSpace.EOSMethod.Lobby;
using Cysharp.Threading.Tasks;
public class CoreSceneEOSOrchestrator : MonoBehaviour
{
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    public void Initialize()
    {
        Debug.Log("EOSへのログインを開始します…");
        EOSLobbyMethod.LoginAsync().Forget();
    }

    public void OnDestroy()
    {
        EOSLobbyMethod.LogoutAsync().Forget();
        EOSLobbyMethod.LeaveLobbyAsync(_eosLobbyOperator.CurrentLobbyId).Forget();
    }
}