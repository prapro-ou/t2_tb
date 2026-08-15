using UnityEngine;
using Cysharp.Threading.Tasks;

public class HomeSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    public void Initialize()
    {
        _sceneBlockTransitionOrchestrator.SceneIn().Forget();
    }
}