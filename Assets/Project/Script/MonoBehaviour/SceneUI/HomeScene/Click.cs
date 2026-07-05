using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using OriginalNameSpace.EOSMethod.Lobby;

public class Click : MonoBehaviour
{
    [SerializeField] private TMP_Text _userName, _roomName;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;

    public void OnClick()
    {
        // TMPのテキストはインプットフィールド経由の場合、末尾に不可視文字が入ることがあるためクレンジング
        string cleanedUserName = _userName.text.Trim().Replace("\u200b", "");
        string cleanedRoomName = _roomName.text.Trim().Replace("\u200b", "");

        LobbyEntry(cleanedUserName, cleanedRoomName).Forget();
    }

    public async UniTask LobbyEntry(string userName, string roomName)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("ユーザー名または部屋名が空です。");
            return;
        }

        // 入室・作成の成否をしっかり受け取る
        bool success = await _eosLobbyOperator.JoinOrCreateRoomAsync(userName, roomName);
        if (!success)
        {
            Debug.LogError("ロビーへの参加または作成に失敗したため、処理を中断します。");
            return;
        }

        // メンバー一覧の取得
        var members = EOSLobbyMethod.GetLobbyMembers(_eosLobbyOperator.LocalProductUserId, _eosLobbyOperator.CurrentLobbyId);
        Debug.Log($"ロビー人数: {members.Count}人");

        // ディスプレイ名の取得
        var displayNames = await EOSLobbyMethod.FetchLobbyDisplayNamesAsync(_eosLobbyOperator.LocalProductUserId, members);

        if (displayNames.TryGetValue(_eosLobbyOperator.LocalProductUserId, out string myName))
        {
            Debug.Log($"自身の表示名: {myName}");
        }
    }

}