using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;

public class HomeSceneLobbyEnterClickOrchestrator : MonoBehaviour
{
    [SerializeField] private TMP_Text _userName, _roomName;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private ProjectOverseer _gameOverseer;
    [SerializeField] private SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    private bool isEnter = false;

    public void OnClick()
    {
        if (isEnter) return;
        isEnter = true;
        // TMPのテキストはインプットフィールド経由の場合、末尾に不可視文字が入ることがあるためクレンジング
        string cleanedUserName = _userName.text.Trim().Replace("\u200b", "");
        string cleanedRoomName = _roomName.text.Trim().Replace("\u200b", "");

        LobbyEntry(cleanedUserName, cleanedRoomName).Forget();
    }

    public async UniTask LobbyEntry(string userName, string roomName)
    {
        try
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning("ユーザー名または部屋名が空です。");
                return;
            }

            // 入室・作成の成否をしっかり受け取る
            bool success = await _eosLobbyOperator.JoinOrCreateRoomAsync(roomName, userName);
            if (!success)
            {
                Debug.LogError("ロビーへの参加または作成に失敗したため、処理を中断します。");
                return;
            }
            await _sceneBlockTransitionOrchestrator.SceneOut();
            await _gameOverseer.sceneOrchestrator.RemoveSceneMediator(SceneNameEnum.HomeScene);
            await _gameOverseer.sceneOrchestrator.AddSceneMediator(SceneNameEnum.LobbyScene);

        }
        finally
        {
            isEnter = false;
        }
    }

}