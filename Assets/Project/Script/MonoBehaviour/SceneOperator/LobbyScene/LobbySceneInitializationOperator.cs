using UnityEngine;
using OriginalNameSpace.EOSMethod.Lobby;
public class LobbySceneInitializationOperator : MonoBehaviour
{
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] LobbySceneLobbyPlayerDisplayOrchestrator _displayOrchestrator;

    public void InitializeLobbyScene()
    {
        var lobbyPlayerNames = EOSLobbyMethod.GetLobbyMemberDisplayNames(_eosLobbyOperator.LocalProductUserId, _eosLobbyOperator.CurrentLobbyId);
        var hostID = EOSLobbyMethod.GetLobbyHostPuid(_eosLobbyOperator.LocalProductUserId, _eosLobbyOperator.CurrentLobbyId);
        _displayOrchestrator.SetPlayerName(lobbyPlayerNames, hostID);
    }
}
