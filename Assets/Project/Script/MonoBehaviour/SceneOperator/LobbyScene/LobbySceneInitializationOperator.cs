using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using OriginalNameSpace.EOSMethod.Lobby;
public class LobbySceneInitializationOperator : MonoBehaviour
{
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    [SerializeField] LobbySceneLobbyPlayerDisplayOrchestrator _displayOrchestrator;
    [SerializeField] LobbySceneModuleCounterOrchestrator _moduleCounterOrchestrator;
    [SerializeField] LobbySceneTimeCounterOrchestrator _timeCounterOrchestrator;
    [SerializeField] LobbySceneP2PConnectOperator _p2pConnectOperator;
    [SerializeField] LobbySceneHostChange _hostChange;
    [SerializeField] TMP_Text _lobbyNameText;

    public void Initialize()
    {
        _sceneBlockTransitionOrchestrator.SceneIn().Forget();
        _lobbyNameText.text = EOSLobbyMethod.GetLobbyName(_eosLobbyOperator.CurrentLobbyId);
        _displayOrchestrator.Initialize();
        _p2pConnectOperator.Initialize();
        _moduleCounterOrchestrator.Initialize();
        _timeCounterOrchestrator.Initialize();
        _hostChange.Initialize();
    }
}
