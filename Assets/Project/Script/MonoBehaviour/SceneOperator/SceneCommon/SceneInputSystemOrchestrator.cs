using UnityEngine;
public class SceneInputSystemOrchestrator : MonoBehaviour
{
    [SerializeField] private ProjectOverseer _gameOverseer;
    [SerializeField] private InputActionAssetEnum _inputActionAssetEnum;
    void Awake()
    {
        _gameOverseer.inputActionAssetOrchestrator.EnabledInputActionAsset(_inputActionAssetEnum);
    }
    void OnDestroy()
    {
        _gameOverseer.inputActionAssetOrchestrator.DisabledInputActionAsset(_inputActionAssetEnum);
    }
}