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
    public async UniTask<bool> InitializeAndLoginAsync()
    {
        // すでにログイン済みの場合はスキップ
        if (LocalProductUserId != null && LocalProductUserId.IsValid())
        {
            return true;
        }

        Debug.Log("EOSへのログインを開始します...");
        LocalProductUserId = await EOSLobbyMethod.LoginAsync();

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
    public async UniTask<bool> JoinOrCreateRoomAsync(string roomName, string userName)
    {
        // ログインしていない場合は先にログインを試みる
        if (LocalProductUserId == null || !LocalProductUserId.IsValid())
        {
            bool loginSuccess = await InitializeAndLoginAsync();
            if (!loginSuccess) return false;
        }

        // すでにどこかのロビーに入っている場合は、一度退出するなどの処理が必要
        if (IsInLobby)
        {
            bool leaveSuccess = await LeaveRoomAsync();
            if (!leaveSuccess)
                return false;
        }

        string lobbyId = await EOSLobbyMethod.JoinOrCreateGameLobbyWithDisplayNameAsync(roomName, userName);

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("ロビーの入室または作成に失敗しました。");
            return false;
        }

        // ロビーIDを保持
        CurrentLobbyId = lobbyId;

        return true;
    }

    public async UniTask<bool> LeaveRoomAsync()
    {
        bool success = await EOSLobbyMethod.LeaveLobbyAsync(CurrentLobbyId);
        if (success) CurrentLobbyId = null;
        return success;
    }
    #region ========== ゲーム入退出処理 ==========

    #region ========== 入出処理 ==========

    private void OnEnable()
    {
        OnEnterGame().Forget();
    }

    private async UniTask OnEnterGame()
    {
        // ScriptableObject有効化タイミングによるID初期化
        LocalProductUserId = null;
        CurrentLobbyId = null;
    }

    #endregion ========== 入出処理 ==========

    #region ========== 退出処理 ==========
    private void OnDisable()
    {
        OnLeaveGame().Forget();
    }

    private async UniTask OnLeaveGame()
    {
        var tmpLobbyId = CurrentLobbyId;
        await EOSLobbyMethod.LeaveLobbyAsync(tmpLobbyId);
        await EOSLobbyMethod.LogoutAsync();
        // ScriptableObject無効化タイミングによるIDクリア
        LocalProductUserId = null;
        CurrentLobbyId = null;
    }

    #endregion ========== 退出処理 ==========

    #endregion ========== ゲーム入退出処理 ==========

}