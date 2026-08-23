using System;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.Lobby;
using OriginalNameSpace.EOSMethod.P2P;
using System.Collections.Generic;
using TMPro;
public class GameSceneStartGameOperator : MonoBehaviour
{
    [SerializeField] TimeModuleTimeDisplayMediator _timeModuleTimeDisplayMediator;
    [SerializeField] EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] GameSettingActiveSOData _gameSettingActiveSOData;
    [SerializeField] ProjectOverseer _projectOverseer;
    [SerializeField] SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    [SerializeField] TMP_Text _text;
    [SerializeField] GameObject _clickBlocker, _textPanel;
    [SerializeField] AudioSource _successAudioSource, _failedAudioSource;
    private bool _isTimer;
    private float _timeCounter;
    private Dictionary<int, bool> _moduleCheck = new Dictionary<int, bool>();
    private string _uuidRadyGame, _uuidModuleCheck;
    public void Initialize()
    {
        _uuidRadyGame = EOSP2PMethod.RegisterListener<StartGamePacket>(OnEndReadyGame);
        for (int i = 0; i < _gameSettingActiveSOData.ThisGameSettingPacket.ModuleCount; i++)
        {
            _moduleCheck.Add(i, false);
        }
        _uuidModuleCheck = EOSP2PMethod.RegisterListener<ModuleCheckPacket>(OnModuleCheck);
    }

    #region ========== 受信 ==========
    /// <summary>
    /// ゲーム開始受信
    /// </summary>
    /// <param name="remoteUserId"></param>
    /// <param name="socketName"></param>
    /// <param name="packet"></param>
    public void OnEndReadyGame(ProductUserId remoteUserId, string socketName, StartGamePacket packet)
    {
        StartGameAsync().Forget();
    }

    /// <summary>
    /// モジュールの確認受信
    /// </summary>
    /// <returns></returns>
    public void OnModuleCheck(ProductUserId remoteUserId, string socketName, ModuleCheckPacket packet)
    {
        switch (packet.CheckType)
        {
            case ModuleCheckEnum.Success:
                _successAudioSource.Play();
                if (_moduleCheck[packet.ModuleNumber]) break;
                _moduleCheck[packet.ModuleNumber] = true;
                if (_moduleCheck.Values.All(x => x == true))
                {
                    EndGame(EndGameTypeEnum.Success).Forget();
                }
                break;
            case ModuleCheckEnum.Failed:
                _failedAudioSource.Play();
                _timeCounter += 15f;
                break;
        }
    }
    #endregion ========== 受信 ==========

    private async UniTask StartGameAsync()
    {
        await EOSLobbyMethod.ConnectToAllLobbyMembersAsync(_eosLobbyOperator.CurrentLobbyId, SocketNameEnum.ModuleInfo);
        await StartGameAnimationAsync();
        GameTimer().Forget();
        _clickBlocker.SetActive(false);
    }

    private async UniTask StartGameAnimationAsync()
    {
        _textPanel.SetActive(true);
        _text.text = "3";
        await UniTask.WaitForSeconds(1f);
        _text.text = "2";
        await UniTask.WaitForSeconds(1f);
        _text.text = "1";
        await UniTask.WaitForSeconds(1f);
        _textPanel.SetActive(false);
    }

    public async UniTask GameTimer()
    {
        _timeCounter = 0;
        _isTimer = true;
        TimeSpan _limitTime = _gameSettingActiveSOData.ThisGameSettingPacket.GameLimitTime;
        while (_timeCounter < _limitTime.TotalSeconds)
        {
            if (!_isTimer) continue;
            _timeCounter += Time.deltaTime;
            _timeModuleTimeDisplayMediator.UpdateTime(_limitTime - TimeSpan.FromSeconds(_timeCounter));
            await UniTask.Yield();
        }
        _timeModuleTimeDisplayMediator.UpdateTime(TimeSpan.Zero);
        EndGame(EndGameTypeEnum.Failed).Forget();
    }

    public void OnDestroy()
    {
        EndGame(EndGameTypeEnum.None).Forget();
    }

    private async UniTask EndGame(EndGameTypeEnum endGameTypeEnum)
    {
        EOSP2PMethod.UnregisterListener(_uuidRadyGame);
        EOSP2PMethod.UnregisterListener(_uuidModuleCheck);
        _isTimer = false;
        EOSP2PMethod.StopListening(SocketNameEnum.ModuleInfo);
        if (endGameTypeEnum == EndGameTypeEnum.None) return;
        _textPanel.SetActive(true);
        switch (endGameTypeEnum)
        {
            case EndGameTypeEnum.Success:
                _text.text = "Success";
                break;
            case EndGameTypeEnum.Failed:
                _text.text = "Failed";
                break;
        }
        await UniTask.WaitForSeconds(3.0f);
        await _sceneBlockTransitionOrchestrator.SceneOut();
        await _projectOverseer.sceneOrchestrator.RemoveSceneMediator(SceneNameEnum.GameScene);
        await _projectOverseer.sceneOrchestrator.AddSceneMediator(SceneNameEnum.LobbyScene);
    }
}