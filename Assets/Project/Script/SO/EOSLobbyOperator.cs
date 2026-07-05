using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.Lobby;

public class EOSLobbyOperator : ScriptableObject
{
    public ProductUserId LocalProductUserId { get; private set; } // 自身のPUDI保持プロパティ
    public string CurrentLobbyId { get; private set; } // 参加ロビーID保持プロパティ
    public bool IsInLobby => !string.IsNullOrEmpty(CurrentLobbyId); // 現在ロビーに参加しているかどうか

    /// <summary>
    /// 初期化ログイン処理を行います
    /// </summary>
    public async UniTask<bool> InitializeAndLoginAsync(string userName)
    {
        // すでにログイン済みの場合はスキップ
        if (LocalProductUserId != null && LocalProductUserId.IsValid())
        {
            return true;
        }

        Debug.Log("EOSへのログインを開始します...");
        LocalProductUserId = await EOSLobbyMethod.LoginAsync(userName);

        if (LocalProductUserId == null)
        {
            Debug.LogError("EOSへのログインに失敗しました。");
            return false;
        }

        Debug.Log($"EOSログイン成功。PUID: {LocalProductUserId}");
        return true;
    }

    /// <summary>
    /// 部屋名を指定して、ロビーへの参加または新規作成を試みます
    /// </summary>
    public async UniTask<bool> JoinOrCreateRoomAsync(string userName, string roomName)
    {
        // ログインしていない場合は先にログインを試みる
        if (LocalProductUserId == null || !LocalProductUserId.IsValid())
        {
            bool loginSuccess = await InitializeAndLoginAsync(userName);
            if (!loginSuccess) return false;
        }

        // すでにどこかのロビーに入っている場合は、一度退出するなどの処理が必要
        if (IsInLobby)
        {
            Debug.LogWarning("すでにロビーに参加しています。新しく入る前に退出してください。");
            return false;
        }

        Debug.Log($"部屋名: {roomName} への入室・作成リクエストを開始します...");
        string lobbyId = await EOSLobbyMethod.JoinOrCreateGameLobbyAsync(roomName);

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("ロビーの入室または作成に失敗しました。");
            return false;
        }

        // ロビーIDを保持
        CurrentLobbyId = lobbyId;
        Debug.Log($"ロビーの確保に成功しました。LobbyID: {CurrentLobbyId}");

        return true;
    }

    private void OnEnable()
    {
        // ScriptableObject有効化タイミングによるID初期化
        LocalProductUserId = null;
        CurrentLobbyId = null;
    }

    private void OnDisable()
    {
        Debug.Log(LocalProductUserId);
        EOSLobbyMethod.LeaveLobbyAsync(LocalProductUserId, CurrentLobbyId).Forget();
        // ScriptableObject無効化タイミングによるIDクリア
        LocalProductUserId = null;
        CurrentLobbyId = null;
    }

}