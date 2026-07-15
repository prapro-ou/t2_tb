using UnityEngine;
using Cysharp.Threading.Tasks;
using OriginalNameSpace.EOSMethod.Lobby;
using OriginalNameSpace.EOSMethod.P2P;
public class LobbySceneStartGameOrchestrator : MonoBehaviour
{
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    public void OnClick()
    {
        Debug.Log("StartGame");
        StartGameAsync().Forget();
    }

    public async UniTask StartGameAsync()
    {
        var packet = new TestPacket
        {
            message = "test"
        };
        var result = EOSLobbyMethod.GetLobbyMembers(_eosLobbyOperator.CurrentLobbyId);
        foreach (var member in result)
        {
            EOSP2PMethod.SendPacket<TestPacket>(SocketNameEnum.Test, member, packet);
        }
    }

}
