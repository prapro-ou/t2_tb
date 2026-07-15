using UnityEngine;
public class LobbySceneInitializationOperator : MonoBehaviour
{
    [SerializeField] LobbySceneLobbyPlayerDisplayOrchestrator _displayOrchestrator;
    [SerializeField] LobbySceneP2PConnectOperator _p2pConnectOperator;

    public void Initialize()
    {
        _displayOrchestrator.Initialize();
        _p2pConnectOperator.Initialize();
    }
}
