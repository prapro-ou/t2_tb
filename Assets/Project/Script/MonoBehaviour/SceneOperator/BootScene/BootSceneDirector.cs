using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
public class BootSceneDirector : MonoBehaviour
{
    [SerializeField] ProjectOverseer _projectOverseer;
    [SerializeField] List<SceneNameEnum> _sceneNames;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Awake()
    {
        Boot().Forget();
    }

    private async UniTask Boot()
    {
        foreach (SceneNameEnum sceneName in _sceneNames)
        {
            await _projectOverseer.sceneOrchestrator.AddSceneMediator(sceneName);
        }
        await _projectOverseer.sceneOrchestrator.RemoveSceneMediator(SceneNameEnum.BootScene);
    }
}
