using UnityEngine;

public class LobbySceneDirector : MonoBehaviour
{
    [SerializeField] private LobbySceneInitializationOperator _lobbySceneInitializationOperator;
    public void Start()
    {
        _lobbySceneInitializationOperator.InitializeLobbyScene();
    }
}
