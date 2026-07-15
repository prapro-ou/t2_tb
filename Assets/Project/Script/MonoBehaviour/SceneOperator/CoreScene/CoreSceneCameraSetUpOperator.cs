using UnityEngine;
public class CoreSceneCameraSetUpOperator : MonoBehaviour
{
    [SerializeField] private GameOverseer _gameOverseer;
    [SerializeField] private Camera _baseCamera;
    public void Initialize()
    {
        BaseCameraSettingOrchestrator();
    }

    /// <summary>
    /// BaseCameraSetupのBaseCameraを設定する
    /// </summary>
    void BaseCameraSettingOrchestrator()
    {
        _gameOverseer.cameraOrchestrator.BaseCameraSetup(_baseCamera);
    }
}