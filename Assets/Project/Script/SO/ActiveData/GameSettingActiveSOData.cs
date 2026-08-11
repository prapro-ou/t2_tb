using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using PlayEveryWare.EpicOnlineServices;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.Lobby;
using OriginalNameSpace.EOSMethod.P2P;

public class GameSettingActiveSOData : ScriptableObject
{
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private AllModuleSOData _allModuleSOData;
    public GameSettingPacket ThisGameSettingPacket { get; set; }
    private bool _isStartGameSetting;
    public bool IsStartGameSetting { get => _isStartGameSetting; }
    private Dictionary<ProductUserId, bool> _choiseDictionary = new Dictionary<ProductUserId, bool>();
    private string _gameSettingEndSignalReceiveUUID;

    #region =========== PublicMethod ==========

    /// <summary>
    /// ゲーム開始受信登録
    /// </summary>
    public void GameSettingEndRegister(List<ProductUserId> userIdList)
    {
        _isStartGameSetting = false;
        _choiseDictionary = userIdList.ToDictionary(x => x, x => false);
        _gameSettingEndSignalReceiveUUID = EOSP2PMethod.RegisterListener<GameSettingEndSignalPacket>(OnGameSettingEnd);
    }

    #endregion ========== PublicMethod ==========



    #region ========== ReciveMethod ==========

    /// <summary>
    /// ゲーム開始設定終了受信
    /// </summary>
    /// <param name="remoteUserId"></param>
    /// <param name="socketName"></param>
    /// <param name="packet"></param>
    private void OnGameSettingEnd(ProductUserId remoteUserId, string socketName, GameSettingEndSignalPacket packet)
    {
        _choiseDictionary[remoteUserId] = true;
        if (_choiseDictionary.Values.All(x => x == true))
        {
            EOSP2PMethod.UnregisterListener(_gameSettingEndSignalReceiveUUID);
            foreach (ProductUserId userId in _choiseDictionary.Keys)
            {
                EOSP2PMethod.SendPacket(SocketNameEnum.Fallback, userId, new StartGamePacket()
                {
                    IsStart = true
                });
            }
            _isStartGameSetting = false;
        }
    }

    #endregion ========== ReciveMethod ==========

    public void OnEnable()
    {
        _isStartGameSetting = false;
        _gameSettingEndSignalReceiveUUID = string.Empty;
    }

    public void OnDisable()
    {
        _isStartGameSetting = false;
        _gameSettingEndSignalReceiveUUID = string.Empty;
    }

}