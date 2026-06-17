using UnityEngine;
public class GameSetUpOperator : MonoBehaviour
{
    [SerializeField] private GameOverseer _gameOverseer;
    [SerializeField] private Camera _baseCamera;
    void Awake()
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