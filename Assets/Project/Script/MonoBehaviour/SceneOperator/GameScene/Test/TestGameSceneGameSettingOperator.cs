using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using PlayEveryWare.EpicOnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class TestGameSceneGameSettingOperator : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("テストしたいSODataを入れる。(VoidとTimeは触らない)")]
    [SerializeField] private SerializedDictionary<ModuleTypeEnum, ModuleSOData> _allModuleSOData;
    [Tooltip("種類数指定")]
    [SerializeField] private int _moduleCounter = 1;
    [Tooltip("時間指定(秒)")]
    [SerializeField] private float _gameLimitTime = 60f;
    [Header("Don't Touch!")]
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;
    [SerializeField] private GameSettingActiveSOData _gameSettingActiveSOData;
    [SerializeField] private Camera _gameCamera;
    [SerializeField] private GameObject _timeObject;
    [SerializeField] private ModuleSOData _timeModuleSOData;
    [SerializeField] private Transform modulePlaceParent;
    [SerializeField] private List<Vector2> modulePlaceList;
    private GameSettingPacket _testGameSettingPacket;

    public async UniTask Initialize(GameSettingPacket testGameSettingPacket)
    {
        EOSP2PMethod.StartListening(SocketNameEnum.ModuleInfo);
        await UniTask.WaitUntil(() => _eosLobbyOperator.LocalProductUserId != null);
        _testGameSettingPacket = testGameSettingPacket;
        ProductUserId userId = _eosLobbyOperator.LocalProductUserId;
        List<ProductUserId> playerIds = _testGameSettingPacket.PlayerUserIds ?? new List<ProductUserId>();
        List<ModuleSettingData> moduleSettingDatas =
            _testGameSettingPacket.ModuleSettingDatas?.GetValueOrDefault(userId)
            ?? new List<ModuleSettingData>();
        List<UniTask> moduleInitializeTasks = new List<UniTask>();
        _gameStatusActiveSOData.Camera = _gameCamera;
        // 1.モジュール確認
        while (moduleSettingDatas.Count < 14)
        {
            moduleSettingDatas.Add(new ModuleSettingData()
            {
                ModuleNumber = -1,
                ModuleType = ModuleTypeEnum.Void,
                ModuleVersion = ModuleVersionEnum.A,
                ModuleGroup = new Dictionary<ModuleVersionEnum, ProductUserId>(),
                ModuleData = new VoidModuleSettingData()
            }
            );
        }
        // 2.モジュール配置
        // 2.0.初期確認
        List<GameObject> modules = new List<GameObject>();
        // 2.1.モジュール配置
        foreach (ModuleSettingData moduleSettingData in moduleSettingDatas.Shuffle())
        {
            GameObject module = Instantiate(
                _allModuleSOData[moduleSettingData.ModuleType].ModulePrefabsDictionary[moduleSettingData.ModuleVersion],
                modulePlaceList[moduleSettingDatas.IndexOf(moduleSettingData)],
                Quaternion.identity,
                modulePlaceParent
            );
            modules.Add(module);
            moduleInitializeTasks.Add(module.GetComponentInChildren<ModuleInitializeOrchestrator>().Initialize(userId, playerIds, _testGameSettingPacket.PlayerColors, moduleSettingData));
        }
        modules.Add(_timeObject);
        Debug.Log(_testGameSettingPacket.GameLimitTime);
        ModuleSettingData timeModuleSettingData = new ModuleSettingData()
        {
            ModuleNumber = -1,
            ModuleType = ModuleTypeEnum.Time,
            ModuleVersion = ModuleVersionEnum.A,
            ModuleGroup = new Dictionary<ModuleVersionEnum, ProductUserId>(),
            ModuleData = new TimeModuleSettingData()
            {
                Time = _testGameSettingPacket.GameLimitTime
            }
        };

        moduleInitializeTasks.Add(_timeObject.GetComponentInChildren<ModuleInitializeOrchestrator>().Initialize(userId, playerIds, _testGameSettingPacket.PlayerColors, timeModuleSettingData));

        // 3.ゲーム準備完了
        await UniTask.WhenAll(moduleInitializeTasks);
        EOSP2PMethod.SendPacket(SocketNameEnum.Fallback, _testGameSettingPacket.HostUserId, new GameSettingEndSignalPacket() { IsSetting = true });
    }

    private void TestDataGenerate()
    {
        int moduleCount = _moduleCounter;
        // 0.初期確認
        GameSettingPacket packet = new GameSettingPacket()
        {
            HostUserId = EOSManager.Instance.GetProductUserId(),
            ModuleCount = moduleCount,
            PlayerUserIds = new List<ProductUserId>() { EOSManager.Instance.GetProductUserId() },
            GameLimitTime = TimeSpan.FromSeconds(_gameLimitTime)
        }; // 送信パケット
        List<ModuleSettingData> moduleSettingDatas = new List<ModuleSettingData>(); // 送信モジュールデータ
        List<ProductUserId> members = new List<ProductUserId>() { EOSManager.Instance.GetProductUserId() };
        List<ProductUserId> choiseMembers = new List<ProductUserId>(); // 選択メンバー
        if (moduleCount < members.Count && 8 * members.Count < moduleCount)
        {
            return;
        }

        // 2.送信モジュールデータ構築
        // 2.1.モジュール選択
        List<ModuleTypeEnum> moduleTypeEnums = _allModuleSOData.Keys.ToList();
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
            _allModuleSOData[ListExtensions.GetRandom(moduleTypeEnums)].GenerateModuleSettingData(ref moduleSettingData);
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
        _gameSettingActiveSOData.GameSettingEndRegister(members);
        _testGameSettingPacket = packet;
    }

}