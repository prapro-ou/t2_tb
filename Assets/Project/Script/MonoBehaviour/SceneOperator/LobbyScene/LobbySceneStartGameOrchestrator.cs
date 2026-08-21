using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using PlayEveryWare.EpicOnlineServices;
using OriginalNameSpace.EOSMethod.Lobby;
using OriginalNameSpace.EOSMethod.P2P;
public class LobbySceneStartGameOrchestrator : MonoBehaviour
{
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private LobbySceneModuleCounterOrchestrator _moduleCounterOrchestrator;
    [SerializeField] private LobbySceneTimeCounterOrchestrator _timeCounterOrchestrator;
    [SerializeField] private AllModuleSOData _allModuleSOData;
    [SerializeField] private ProjectOverseer _gameOverseer;
    [SerializeField] private GameSettingActiveSOData _gameSettingActiveSOData;
    [SerializeField] private GameObject _blockPanel;

    public void OnClick()
    {
        HostStartGame();
        _blockPanel.SetActive(true);
    }

    // public async UniTask HostStartGameTest()
    // {
    //     List<ProductUserId> members = EOSLobbyMethod.GetLobbyMembers(_eosLobbyOperator.CurrentLobbyId); // ロビーメンバー
    //     foreach (ProductUserId userId in members)
    //     {
    //         Debug.Log(userId + "に送信");
    //         EOSP2PMethod.SendPacket(SocketNameEnum.Fallback, userId, new TestLogPacket());
    //     }
    //     await UniTask.WaitForSeconds(10.0f);
    //     HostStartGame();
    // }
    /// <summary>
    /// ホストがゲームを開始する
    /// </summary>
    /// <param name="moduleCount"></param>
    public void HostStartGame()
    {
        int moduleCount = _moduleCounterOrchestrator.ModuleCounter;
        if (_gameSettingActiveSOData.IsStartGameSetting) return;
        // 0.初期確認
        GameSettingPacket packet = new GameSettingPacket()
        {
            HostUserId = EOSManager.Instance.GetProductUserId(),
            ModuleCount = moduleCount,
            PlayerUserIds = EOSLobbyMethod.GetLobbyMembers(_eosLobbyOperator.CurrentLobbyId),
            GameLimitTime = TimeSpan.FromSeconds(_timeCounterOrchestrator.TimeCounter * 15)
        }; // 送信パケット
        Debug.Log($"ゲーム開始。モジュール数：{moduleCount} 時間：{_timeCounterOrchestrator.TimeCounter}分");
        List<ModuleSettingData> moduleSettingDatas = new List<ModuleSettingData>(); // 送信モジュールデータ
        List<ProductUserId> members = EOSLobbyMethod.GetLobbyMembers(_eosLobbyOperator.CurrentLobbyId); // ロビーメンバー
        List<ProductUserId> choiseMembers = new List<ProductUserId>(); // 選択メンバー
        if (moduleCount < members.Count && 8 * members.Count < moduleCount)
        {
            return;
        }

        // 2.送信モジュールデータ構築
        // 2.1.モジュール選択
        List<ModuleTypeEnum> moduleTypeEnums = _allModuleSOData.ModuleDatas.Keys.ToList();
        List<ModuleTypeEnum> removeList = new List<ModuleTypeEnum>()
        {
            ModuleTypeEnum.None,
            ModuleTypeEnum.Void,
            ModuleTypeEnum.Time
        };
        moduleTypeEnums = moduleTypeEnums.Except(removeList).ToList();

        // 2.2.モジュールデータ生成
        for (int i = 0; i < moduleCount; i++)
        {
            ModuleSettingData moduleSettingData = new ModuleSettingData()
            {
                ModuleNumber = i
            };
            _allModuleSOData.ModuleDatas[ListExtensions.GetRandom(moduleTypeEnums)].GenerateModuleSettingData(ref moduleSettingData);
            moduleSettingDatas.Add(moduleSettingData);
            Debug.Log(moduleSettingData.ModuleType);
        }

        // 3.メンバー順番決め
        System.Random rand = new System.Random();
        choiseMembers = Enumerable.Range(0, moduleCount * 2)
                .Select(i => members[i % members.Count])
                .ToList();
        Queue<ProductUserId> queueMembers = new Queue<ProductUserId>(choiseMembers);

        // 4. データ生成
        packet.ModuleSettingDatas = new Dictionary<ProductUserId, List<ModuleSettingData>>(); // 送信パケット
        foreach (var memberId in members) // ロビーメンバー
        {
            packet.ModuleSettingDatas[memberId] = new List<ModuleSettingData>();
        }

        // 5. 色構築
        List<Color> colors = new List<Color>()
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
        };

        colors = colors.Shuffle();
        Dictionary<ProductUserId, Color> playerColors = new Dictionary<ProductUserId, Color>();
        for (int i = 0; i < members.Count; i++)
        {
            playerColors[members[i]] = colors[i];
        }
        packet.PlayerColors = playerColors;

        // 6.データ構築
        for (int i = 0; i < moduleSettingDatas.Count; i++)
        {
            ModuleSettingData originalData = moduleSettingDatas[i]; // 元データ
            if (originalData.ModuleGroup == null) continue;

            // 6.1.担当者情報
            foreach (ModuleVersionEnum versionKey in originalData.ModuleGroup.Keys.ToList().Shuffle())
            {
                ProductUserId ownerId = queueMembers.Dequeue();
                originalData.ModuleGroup[versionKey] = ownerId;
            }

            // 6.2.モジュール情報分配
            foreach (var kvp in originalData.ModuleGroup)
            {
                ModuleVersionEnum versionKey = kvp.Key;
                ProductUserId ownerId = kvp.Value;
                ModuleSettingData playerModuleData = new ModuleSettingData
                {
                    ModuleNumber = originalData.ModuleNumber,
                    ModuleType = originalData.ModuleType,
                    ModuleVersion = versionKey,
                    ModuleGroup = originalData.ModuleGroup,
                    ModuleData = originalData.ModuleData
                };

                // 担当ユーザーのリストに追加
                packet.ModuleSettingDatas[ownerId].Add(playerModuleData);
            }

            // 元のリストも更新しておく
            moduleSettingDatas[i] = originalData;
        }

        // 6. モジュール情報送信
        _gameSettingActiveSOData.GameSettingEndRegister(members);
        foreach (ProductUserId userId in members)
        {
            Debug.Log(userId + " " + packet);
            EOSP2PMethod.SendPacketSafeAsync(SocketNameEnum.Fallback, userId, packet).Forget();
        }
    }
}