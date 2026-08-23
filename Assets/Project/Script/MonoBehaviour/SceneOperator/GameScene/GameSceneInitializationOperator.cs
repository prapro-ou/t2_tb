using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class GameSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private bool _isTest = false;
    [SerializeField] private GameSceneGameSettingOperator _gameSceneGameSettingOperator;
    [SerializeField] private GameSceneStartGameOperator _gameSceneStartGameOperator;
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    [SerializeField] private BGMOperator _bgmOperator;
    [SerializeField] private AudioClip _bgmClip;

    public void Initialize()
    {
        _gameSceneStartGameOperator.Initialize();
        _sceneBlockTransitionOrchestrator.SceneIn().Forget();
        _gameSceneGameSettingOperator.Initialize().Forget();
        _bgmOperator.SetPlay(_bgmClip);
    }
}