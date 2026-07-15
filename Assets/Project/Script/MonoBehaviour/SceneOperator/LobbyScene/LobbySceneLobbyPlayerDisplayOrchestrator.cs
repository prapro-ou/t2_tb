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
    #endregion ========== 定数・変数 ==========

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        var lobbyPlayerNames = EOSLobbyMethod.GetLobbyMemberDisplayNames(_eosLobbyOperator.CurrentLobbyId);
        var hostID = EOSLobbyMethod.GetLobbyHostPuid(_eosLobbyOperator.CurrentLobbyId);
        SetPlayerName(lobbyPlayerNames, hostID);
        EOSLobbyMethod.RegisterLobbyNotifications(_eosLobbyOperator.CurrentLobbyId, LobbyNoticeEnum.LobbyDisplayChange, LobbyMemberDisplay);
    }

    /// <summary>
    /// 名前表示
    /// </summary>
    /// <param name="nameDictionary"></param>
    /// <param name="hostID"></param>
    private void SetPlayerName(Dictionary<ProductUserId, string> nameDictionary, ProductUserId hostID)
    {
        foreach (KeyValuePair<ProductUserId, string> name in nameDictionary)
        {
            GameObject panel = Instantiate(_displayPanel, Vector3.zero, Quaternion.identity, _displayField.transform);//パネル生成
            panel.GetComponentInChildren<LobbyUserDisplayOperator>()?.InitializeLobbyUserDisplay(name.Value, name.Key == hostID);
        }
    }

    public void OnDestroy()
    {
        EOSLobbyMethod.UnregisterLobbyNotifications(LobbyNoticeEnum.LobbyDisplayChange);
    }

    #region ========== Lobby入退出処理 ==========
    public void LobbyMemberDisplay(LobbyMemberStatusReceivedCallbackInfo info)
    {
        SetPlayerName(EOSLobbyMethod.GetLobbyMemberDisplayNames(_eosLobbyOperator.CurrentLobbyId), EOSLobbyMethod.GetLobbyHostPuid(_eosLobbyOperator.CurrentLobbyId));
    }

    #endregion
}