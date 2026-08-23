using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class TestGameSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private bool _isTest = false;
    [SerializeField] private TestGameSceneGameSettingOperator _testGameSceneGameSettingOperator;
    [SerializeField] private GameSceneStartGameOperator _gameSceneStartGameOperator;
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;


    public void Initialize()
    {
        _gameSceneStartGameOperator.Initialize();
        _sceneBlockTransitionOrchestrator.SceneIn().Forget();
        _testGameSceneGameSettingOperator.Initialize().Forget();

    }
}