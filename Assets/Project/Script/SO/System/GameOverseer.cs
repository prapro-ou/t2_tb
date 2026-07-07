using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
/// <summary>
/// 現在のゲームの状況を把握し、CoreSceneとの連携を行う
/// 各Sceneの権限を統括するもの
/// </summary>
public class GameOverseer : ScriptableObject
{
    public SceneOrchestrator sceneOrchestrator;
    public InputActionAssetOrchestrator inputActionAssetOrchestrator;
    public CameraOrchestrator cameraOrchestrator;

    /// <summary>
    /// シーン管理を行うメソッドをまとめたクラス
    /// </summary>
    [System.Serializable]
    public class SceneOrchestrator
    {
        private SerializedDictionary<SceneNameEnum, bool> _scenesBoolDictionary = new SerializedDictionary<SceneNameEnum, bool>();
        private List<SceneNameEnum> _currentSceneName;
        public void AddSceneMediator(SceneNameEnum addSceneName)
        {
            SceneManager.LoadSceneAsync(addSceneName.ToString(), LoadSceneMode.Additive);
        }
        public void RemoveSceneMediator(SceneNameEnum removeSceneName)
        {
            SceneManager.UnloadSceneAsync(removeSceneName.ToString());
        }
    }

    /// <summary>
    /// アクションマップ管理を行うメソッドをまとめたクラス
    /// </summary>
    [System.Serializable]
    public class InputActionAssetOrchestrator
    {
        [SerializeField] private SerializedDictionary<InputActionAssetEnum, InputActionAsset> _inputActionAssetDictionary = new SerializedDictionary<InputActionAssetEnum, InputActionAsset>();
        public void EnabledInputActionAsset(InputActionAssetEnum enableInputActionAsset)
        {
            _inputActionAssetDictionary[enableInputActionAsset].Enable();
        }
        public void DisabledInputActionAsset(InputActionAssetEnum disableInputActionAsset)
        {
            _inputActionAssetDictionary[disableInputActionAsset].Disable();
        }
    }

    /// <summary>
    /// カメラ管理を行うメソッドをまとめたクラス
    /// </summary>
    [System.Serializable]
    public class CameraOrchestrator
    {
        private Camera _baseCamera; //基底カメラ
        /// <summary>
        /// 既定カメラをセットするメソッド
        /// </summary>
        public void BaseCameraSetup(Camera camera) => _baseCamera = camera;

        /// <summary>
        /// 既定カメラに各セクションのカメラを追加するメソッド
        /// </summary>
        /// <param name="addCameras">追加するカメラ</param>
        /// <returns>UniTask</returns>
        public async UniTask AddCameraMediator(List<Camera> addCameras)
        {
            await UniTask.WaitUntil(() => _baseCamera != null);
            List<Camera> baseCameraStack = _baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            foreach (Camera overlayCamera in addCameras)
            {
                if (overlayCamera != null && overlayCamera.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay)
                {
                    baseCameraStack.Add(overlayCamera);//カメラ追加
                }
            }
        }

        /// <summary>
        /// 基底カメラから各セクションのカメラを取り除くメソッド
        /// </summary>
        /// <param name="removeCameras">取り除くカメラ</param>
        /// <returns>UniTask</returns>
        public async UniTask RemoveCameraMediator(List<Camera> removeCameras)
        {
            await UniTask.WaitUntil(() => _baseCamera != null);
            List<Camera> baseCameraStack = _baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            foreach (Camera overlayCamera in removeCameras)
            {
                if (overlayCamera != null && overlayCamera.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay)
                {
                    baseCameraStack.Remove(overlayCamera); // カメラ削除
                }
            }
        }
    }
}