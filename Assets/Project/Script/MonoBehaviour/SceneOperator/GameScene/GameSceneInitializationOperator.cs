using UnityEngine;

public class GameSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private GameSceneGameSettingOperator _gameSceneGameSettingOperator;
    public void Initialize()
    {
        _gameSceneGameSettingOperator.Initialize();
    }
}