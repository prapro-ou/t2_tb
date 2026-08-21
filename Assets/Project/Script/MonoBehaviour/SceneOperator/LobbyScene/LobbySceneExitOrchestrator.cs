using UnityEngine;
using Cysharp.Threading.Tasks;
public class LobbySceneExitOrchestrator : MonoBehaviour
{
    [SerializeField] private ProjectOverseer _projectOverseer;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    [SerializeField] private GameObject _blockPanel;
    [SerializeField] private AudioSource _audioSource;

    public void OnClick()
    {
        ExitLobbyScene().Forget();
    }

    private async UniTask ExitLobbyScene()
    {
        bool success = await _eosLobbyOperator.LeaveRoomAsync();
        if (!success) return;
        _blockPanel.SetActive(true);
        _audioSource.Play();
        await _sceneBlockTransitionOrchestrator.SceneOut();
        await _projectOverseer.sceneOrchestrator.RemoveSceneMediator(SceneNameEnum.LobbyScene);
        await _projectOverseer.sceneOrchestrator.AddSceneMediator(SceneNameEnum.HomeScene);
    }
}