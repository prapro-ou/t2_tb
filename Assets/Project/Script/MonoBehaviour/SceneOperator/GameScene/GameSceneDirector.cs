using UnityEngine;

public class GameSceneDirector : MonoBehaviour
{
    [SerializeField] GameSceneInitializationOperator _gameSceneInitializationOperator;
    void Start()
    {
        _gameSceneInitializationOperator.Initialize();

    }
}
