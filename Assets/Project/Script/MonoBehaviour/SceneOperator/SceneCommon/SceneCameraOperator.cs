using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
public class SceneCameraOperator : MonoBehaviour
{
    [SerializeField] private GameOverseer _gameOverseer;
    [SerializeField] private List<Camera> _overlayCamera;
    void Awake()
    {
        _gameOverseer.cameraOrchestrator.AddCameraMediator(_overlayCamera).Forget();
    }
    void OnDestroy()
    {
        _gameOverseer.cameraOrchestrator.RemoveCameraMediator(_overlayCamera).Forget();
    }
}