using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class LobbySceneP2PConnectOperator : MonoBehaviour
{
    [SerializeField] private ProjectOverseer _projectOverseer;
    [SerializeField] private GameSettingActiveSOData _gameStatusActiveSOData;
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    private string _gameSettingPacketReceiveUUID;
    public void Initialize()
    {
        _gameSettingPacketReceiveUUID = EOSP2PMethod.RegisterListener<GameSettingPacket>(OnGameSettingPacketReceived);
    }

    private void OnGameSettingPacketReceived(ProductUserId remoteUserId, string socketName, GameSettingPacket packet)
    {
        _gameStatusActiveSOData.ThisGameSettingPacket = packet;
        GameSceneChange().Forget();
    }

    private async UniTask GameSceneChange()
    {
        await _sceneBlockTransitionOrchestrator.SceneOut();
        await _projectOverseer.sceneOrchestrator.RemoveSceneMediator(SceneNameEnum.LobbyScene);
        await _projectOverseer.sceneOrchestrator.AddSceneMediator(SceneNameEnum.GameScene);
        EOSP2PMethod.UnregisterListener(_gameSettingPacketReceiveUUID);
    }

    public void OnDisable()
    {
        EOSP2PMethod.UnregisterListener(_gameSettingPacketReceiveUUID);
    }
}