using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;

public class GameSceneGameSettingOperator : MonoBehaviour
{
    [SerializeField] private AllModuleSOData _allModuleSOData;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private GameSettingActiveSOData _gameSettingActiveSOData;
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;
    [SerializeField] private Camera _gameCamera;
    [SerializeField] private GameObject _timeObject;
    [SerializeField] private ModuleSOData _timeModuleSOData;
    [SerializeField] private Transform modulePlaceParent;
    [SerializeField] private List<Vector2> modulePlaceList;

    public async UniTask Initialize()
    {
        await UniTask.WaitUntil(() => _eosLobbyOperator.LocalProductUserId != null);
        ProductUserId userId = _eosLobbyOperator.LocalProductUserId;
        List<ProductUserId> playerIds = _gameSettingActiveSOData?.ThisGameSettingPacket.PlayerUserIds ?? new List<ProductUserId>();
        List<ModuleSettingData> moduleSettingDatas =
            _gameSettingActiveSOData?.ThisGameSettingPacket.ModuleSettingDatas?.GetValueOrDefault(userId)
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
        List<Object> modules = new List<Object>();
        // 2.1.モジュール配置
        foreach (ModuleSettingData moduleSettingData in moduleSettingDatas.Shuffle())
        {
            GameObject module = Instantiate(
                _allModuleSOData.ModuleDatas[moduleSettingData.ModuleType].ModulePrefabsDictionary[moduleSettingData.ModuleVersion],
                modulePlaceList[moduleSettingDatas.IndexOf(moduleSettingData)],
                Quaternion.identity,
                modulePlaceParent
            );
            modules.Add(module);
            moduleInitializeTasks.Add(module.GetComponentInChildren<ModuleInitializeOrchestrator>().Initialize(userId, playerIds, _gameSettingActiveSOData.ThisGameSettingPacket.PlayerColors, moduleSettingData));
        }
        modules.Add(_timeObject);
        Debug.Log(_gameSettingActiveSOData.ThisGameSettingPacket.GameLimitTime);
        ModuleSettingData timeModuleSettingData = new ModuleSettingData()
        {
            ModuleNumber = -1,
            ModuleType = ModuleTypeEnum.Time,
            ModuleVersion = ModuleVersionEnum.A,
            ModuleGroup = new Dictionary<ModuleVersionEnum, ProductUserId>(),
            ModuleData = new TimeModuleSettingData()
            {
                Time = _gameSettingActiveSOData.ThisGameSettingPacket.GameLimitTime
            }
        };

        moduleInitializeTasks.Add(_timeObject.GetComponentInChildren<ModuleInitializeOrchestrator>().Initialize(userId, playerIds, _gameSettingActiveSOData.ThisGameSettingPacket.PlayerColors, timeModuleSettingData));

        // 3.ゲーム準備完了
        await UniTask.WhenAll(moduleInitializeTasks);
        EOSP2PMethod.SendPacket(SocketNameEnum.Fallback, _gameSettingActiveSOData.ThisGameSettingPacket.HostUserId, new GameSettingEndSignalPacket() { IsSetting = true });
    }

    private void TestDataGenerate()
    {
        _gameSettingActiveSOData.ThisGameSettingPacket = new GameSettingPacket()
        {
            HostUserId = _eosLobbyOperator.LocalProductUserId,
            ModuleSettingDatas = new Dictionary<ProductUserId, List<ModuleSettingData>>()
            {
                { _eosLobbyOperator.LocalProductUserId, new List<ModuleSettingData>() }
            },
            PlayerColors = new Dictionary<ProductUserId, Color>()
            {
                { _eosLobbyOperator.LocalProductUserId, Color.red }
            }
        };
    }

}