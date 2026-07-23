using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class LobbySceneP2PConnectOperator : MonoBehaviour
{
    [SerializeField] private ProjectOverseer _gameOverseer;
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;
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
        await _gameOverseer.sceneOrchestrator.RemoveSceneMediator(SceneNameEnum.LobbyScene);
        await _gameOverseer.sceneOrchestrator.AddSceneMediator(SceneNameEnum.GameScene);
        EOSP2PMethod.UnregisterListener(_gameSettingPacketReceiveUUID);
    }

    public void OnDisable()
    {
        EOSP2PMethod.UnregisterListener(_gameSettingPacketReceiveUUID);
    }
}