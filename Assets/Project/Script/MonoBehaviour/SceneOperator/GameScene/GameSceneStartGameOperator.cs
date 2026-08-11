using System;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
using System.Collections.Generic;
using TMPro;
public class GameSceneStartGameOperator : MonoBehaviour
{
    [SerializeField] TimeModuleTimeDisplayMediator _timeModuleTimeDisplayMediator;
    [SerializeField] GameSettingActiveSOData _gameSettingActiveSOData;
    [SerializeField] ProjectOverseer _projectOverseer;
    [SerializeField] SceneBlockTransitionOrchestrator _sceneBlockTransitionOrchestrator;
    [SerializeField] TMP_Text _endText;
    [SerializeField] GameObject _clickBlocker;
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
                if (_moduleCheck[packet.ModuleNumber]) break;
                _moduleCheck[packet.ModuleNumber] = true;
                if (_moduleCheck.Values.All(x => x == true))
                {
                    EndGame(EndGameTypeEnum.Success).Forget();
                }
                break;
            case ModuleCheckEnum.Failed:
                _timeCounter += 15;
                Debug.Log("ペナルティ");
                break;
        }
    }
    #endregion ========== 受信 ==========

    private async UniTask StartGameAsync()
    {
        GameTimer().Forget();
        _clickBlocker.SetActive(false);
        EOSP2PMethod.StartListening(SocketNameEnum.ModuleInfo);
    }

    public async UniTask GameTimer()
    {
        _timeCounter = 0;
        TimeSpan _limitTime = _gameSettingActiveSOData.ThisGameSettingPacket.GameLimitTime;
        while (_timeCounter < _limitTime.TotalSeconds)
        {
            _timeCounter += Time.deltaTime;
            _timeModuleTimeDisplayMediator.UpdateTime(_limitTime - TimeSpan.FromSeconds(_timeCounter));
            await UniTask.Yield();
        }
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
        EOSP2PMethod.StopListening(SocketNameEnum.ModuleInfo);
        if (endGameTypeEnum == EndGameTypeEnum.None) return;
        switch (endGameTypeEnum)
        {
            case EndGameTypeEnum.Success:
                _endText.text = "Success";
                break;
            case EndGameTypeEnum.Failed:
                _endText.text = "Failed";
                break;
        }
        await UniTask.WaitForSeconds(3.0f);
        await _sceneBlockTransitionOrchestrator.SceneOut();
        await _projectOverseer.sceneOrchestrator.RemoveSceneMediator(SceneNameEnum.GameScene);
        await _projectOverseer.sceneOrchestrator.AddSceneMediator(SceneNameEnum.LobbyScene);
    }
}