using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class GameSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private GameSceneGameSettingOperator _gameSceneGameSettingOperator;
    [SerializeField] private GameSceneStartGameOperator _gameSceneStartGameOperator;
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;

    public void Initialize()
    {
        _gameSceneStartGameOperator.Initialize();
        _sceneBlockTransitionOrchestrator.SceneIn().Forget();
        _gameSceneGameSettingOperator.Initialize().Forget();
    }
}