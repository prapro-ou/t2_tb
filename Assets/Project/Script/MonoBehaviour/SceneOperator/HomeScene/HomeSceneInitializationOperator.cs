using UnityEngine;
using Cysharp.Threading.Tasks;

public class HomeSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    [SerializeField] private BGMOperator _bgmOperator;
    [SerializeField] private AudioClip _bgmClip;
    public void Initialize()
    {
        _sceneBlockTransitionOrchestrator.SceneIn().Forget();
        _bgmOperator.SetPlay(_bgmClip);
    }
}