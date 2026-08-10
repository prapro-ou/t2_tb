using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using OriginalNameSpace.EOSMethod.Lobby;
public class LobbySceneLobbyPlayerDisplayOrchestrator : MonoBehaviour
{
    #region ========== 定数・変数 ==========
    [SerializeField] private GameObject _displayField;
    [SerializeField] private GameObject _displayPanel;
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    private ulong _notificationId = 0;
    #endregion ========== 定数・変数 ==========

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        // 0. 初期確認
        var lobbyPlayerNames = EOSLobbyMethod.GetLobbyMemberDisplayNames(_eosLobbyOperator.CurrentLobbyId);
        var hostID = EOSLobbyMethod.GetLobbyHostPuid(_eosLobbyOperator.CurrentLobbyId);

        // 1. 名前表示
        SetPlayerName(lobbyPlayerNames, hostID, _displayField, _displayPanel);

        // 2. 通知登録
        _notificationId = EOSLobbyMethod.RegisterLobbyNotifications(_eosLobbyOperator.CurrentLobbyId, LobbyMemberDisplay);
    }

    #region ========== サイクル処理 ==========
    public void OnDestroy()
    {
        EOSLobbyMethod.UnregisterLobbyNotifications(_notificationId);
    }
    #endregion ========== サイクル処理 ==========



    #region ========== Lobby入退出処理 ==========
    public void LobbyMemberDisplay(LobbyMemberStatusReceivedCallbackInfo info)
    {
        var lobbyPlayerNames = EOSLobbyMethod.GetLobbyMemberDisplayNames(_eosLobbyOperator.CurrentLobbyId);
        var targetID = info.TargetUserId;
        switch (info.CurrentStatus)
        {

            case LobbyMemberStatus.Joined:
                AddUser(lobbyPlayerNames, targetID, _displayField, _displayPanel);
                break;

            case LobbyMemberStatus.Left:
                RemoveUser(targetID, _displayField);
                break;

            case LobbyMemberStatus.Disconnected:
                RemoveUser(targetID, _displayField);
                break;

            case LobbyMemberStatus.Kicked:
                RemoveUser(targetID, _displayField);
                break;

            case LobbyMemberStatus.Promoted:
                ChangeHost(targetID, _displayField);
                break;

            default:
                break;
        }
    }
    #endregion ========== Lobby入退出処理 ==========



    #region ========== 表示処理 ==========
    /// <summary>
    /// 初期名前表示
    /// </summary>
    /// <param name="nameDictionary">ロビー参加者名の辞書</param>
    /// <param name="hostID">ロビーホストID</param>
    /// <param name="displayField">表示場所</param>
    /// <param name="displayPanel">表示パネル</param>
    private void SetPlayerName(Dictionary<ProductUserId, string> nameDictionary, ProductUserId hostID, GameObject displayField, GameObject displayPanel)
    {
        displayField.transform.DestroyAllChildren();//子オブジェクト破棄
        foreach (KeyValuePair<ProductUserId, string> name in nameDictionary)
        {
            GameObject panel = Instantiate(displayPanel, Vector3.zero, Quaternion.identity, displayField.transform);//パネル生成
            panel.GetComponentInChildren<LobbyUserDisplayOperator>()?.InitializeLobbyUserDisplay(name.Key, name.Value, name.Key == hostID);
        }
    }

    /// <summary>
    /// ユーザー参加
    /// </summary>
    /// <param name="nameDictionary">ロビー参加者名の辞書</param>
    /// <param name="targetID">ロビー参加者ID</param>
    /// <param name="displayField">表示場所</param>
    /// <param name="displayPanel">表示パネル</param>
    public void AddUser(Dictionary<ProductUserId, string> nameDictionary, ProductUserId targetID, GameObject displayField, GameObject displayPanel)
    {
        string targetName = nameDictionary[targetID];
        GameObject panel = Instantiate(displayPanel, Vector3.zero, Quaternion.identity, displayField.transform);//パネル生成
        panel.GetComponentInChildren<LobbyUserDisplayOperator>()?.InitializeLobbyUserDisplay(targetID, targetName, false);
    }

    /// <summary>
    /// ユーザー退室
    /// </summary>
    /// <param name="nameDictionary">ロビー参加者名の辞書</param>
    /// <param name="targetID">ロビー退室者ID</param>
    /// <param name="displayField">表示場所</param>
    public void RemoveUser(ProductUserId targetID, GameObject displayField)
    {
        foreach (Transform child in displayField.transform)
        {
            if (child.GetComponent<LobbyUserDisplayOperator>()?.UserId == targetID)
            {
                Destroy(child.gameObject);
                break;
            }
        }
    }

    /// <summary>
    /// ホスト変更
    /// </summary>
    /// <param name="hostID">ロビーホストID</param>
    /// <param name="displayField">表示場所</param>
    public void ChangeHost(ProductUserId hostID, GameObject displayField)
    {
        foreach (Transform child in displayField.transform)
        {
            LobbyUserDisplayOperator displayOperator = child.GetComponent<LobbyUserDisplayOperator>();
            if (hostID == displayOperator.UserId)
            {
                displayOperator.HostChange(true);
            }
            else if (hostID != displayOperator.UserId && displayOperator.IsHost)
            {
                displayOperator.HostChange(false);
            }
        }
    }

    #endregion ========== 表示処理 ==========

}