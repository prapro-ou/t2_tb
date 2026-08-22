using Cysharp.Threading.Tasks;
using UnityEngine;

public class CoreSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private CoreSceneCameraSetUpOperator _cameraSetUpOperator;
    [SerializeField] private CoreSceneEOSOrchestrator _eosOrchestrator;
    [SerializeField] private CoreSceneBGMOperator _bgmOperator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Intialize()
    {
        _cameraSetUpOperator.Initialize();
        _bgmOperator.Initialize();
        _eosOrchestrator.Initialize().Forget();
    }
}
