using UnityEngine;

public class CoreSceneInitializationOperator : MonoBehaviour
{
    [SerializeField] private CoreSceneCameraSetUpOperator _cameraSetUpOperator;
    [SerializeField] private CoreSceneEOSOrchestrator _eosOrchestrator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Intialize()
    {
        _cameraSetUpOperator.Initialize();
        _eosOrchestrator.Initialize();
    }
}
