using UnityEngine;
using Epic.OnlineServices;              // SDKコア（Result, ProductUserId用）
using Epic.OnlineServices.Lobby;        // ロビー機能用
using PlayEveryWare.EpicOnlineServices; // EOSManager用

public class EOSLobbyManager : MonoBehaviour
{
    private LobbyInterface lobbyInterface;
    private string currentLobbyId;

    void Start()
    {
        // ログイン成功後に、EOSManagerからLobbyInterfaceを取得しておく
        var platformInterface = EOSManager.Instance.GetEOSPlatformInterface();
        if (platformInterface != null)
        {
            lobbyInterface = platformInterface.GetLobbyInterface();
        }
    }

    // クライアントに伝える「合言葉」を指定してロビーを作る関数
    public void CreatePrivateLobby(string roomPassword)
    {
        if (lobbyInterface == null)
        {
            Debug.LogError("Lobby Interfaceが初期化されていません。");
            return;
        }

        // 1. ロビーの作成オプションを設定
        var createLobbyOptions = new CreateLobbyOptions
        {
            LocalUserId = EOSManager.Instance.GetProductUserId(), // ログインで取得したProductUserId
            MaxLobbyMembers = 4,                               // 最大参加人数（例: 4人）
            PermissionLevel = LobbyPermissionLevel.Publicadvertised, // ★合言葉で検索できるようにPublicにする
            PresenceEnabled = true
        };

        // 2. ロビー作成リクエストを送信
        lobbyInterface.CreateLobby(ref createLobbyOptions, null, (ref CreateLobbyCallbackInfo callbackInfo) =>
        {
            // Result.Success で作成成功
            if (callbackInfo.ResultCode == Result.Success)
            {
                Debug.Log($"ロビーの作成に成功しました。 LobbyID: {callbackInfo.LobbyId}");
                currentLobbyId = callbackInfo.LobbyId;

                // 3. ロビーが作れたら、続けて「合言葉」をロビーに登録する
                SetLobbyPassword(callbackInfo.LobbyId, roomPassword);
            }
            else
            {
                Debug.LogError($"ロビー作成失敗: {callbackInfo.ResultCode}");
            }
        });
    }

    // 作成したロビーに「合言葉」の属性を付与する関数
    private void SetLobbyPassword(string lobbyId, string password)
    {
        // ロビー変更用のオプション
        var updateLobbyModificationOptions = new UpdateLobbyModificationOptions
        {
            LobbyId = lobbyId,
            LocalUserId = EOSManager.Instance.GetProductUserId(),
        };

        // ロビーのデータを編集するための「ハンドル（編集権）」を取得
        Result result = lobbyInterface.UpdateLobbyModification(ref updateLobbyModificationOptions, out LobbyModification lobbyModificationHandle);

        if (result == Result.Success)
        {
            // カスタム属性（Attribute）として「合言葉」のデータを作成
            var attributeData = new AttributeData
            {
                Key = "ROOM_PASSWORD", // クライアントが検索するときのキー（共通の文字列にする）
                Value = new AttributeDataValue { AsUtf8 = password } // プレイヤーが入力した実際の合言葉
            };

            var addAttributeOptions = new LobbyModificationAddAttributeOptions
            {
                Attribute = attributeData,
                Visibility = LobbyAttributeVisibility.Public // ★外部から検索（フィルタリング）できるようにPublicにする
            };

            // ハンドルに対して属性を追加
            lobbyModificationHandle.AddAttribute(ref addAttributeOptions);

            // 4. 変更を確定（コミット）してEOSサーバーに反映する
            var updateLobbyOptions = new UpdateLobbyOptions
            {
                LobbyModificationHandle = lobbyModificationHandle
            };

            lobbyInterface.UpdateLobby(ref updateLobbyOptions, null, (ref UpdateLobbyCallbackInfo callbackInfo) =>
            {
                if (callbackInfo.ResultCode == Result.Success)
                {
                    Debug.Log($"ロビーに合言葉 '{password}' を設定しました！クライアントの接続を待ちます。");
                }
                else
                {
                    Debug.LogError($"ロビー属性の更新に失敗しました: {callbackInfo.ResultCode}");
                }

                // 使用したハンドルは必ず解放（Release）する
                lobbyModificationHandle.Release();
            });
        }
    }
}