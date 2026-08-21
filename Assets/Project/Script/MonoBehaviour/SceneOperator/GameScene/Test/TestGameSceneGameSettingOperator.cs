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
    [Tooltip("テストしたいSODataを入れる")]
    [SerializeField] private ModuleSOData _moduleSOData;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;
    [SerializeField] private GameSettingActiveSOData _gameSettingActiveSOData;
    [SerializeField] private Camera _gameCamera;
    [SerializeField] private Transform modulePlaceParent;
    [SerializeField]
    private List<Vector2> modulePlaceList = new List<Vector2>()
    {

        new Vector2(200f, 0f),
        new Vector2(-200f, 0f),
    };
    private GameSettingPacket _testGameSettingPacket;
    public void Start()
    {
        Initialize().Forget();
    }

    public async UniTask Initialize()
    {
        await UniTask.WaitUntil(() => _eosLobbyOperator.LocalProductUserId != null);
        EOSP2PMethod.StartListening(SocketNameEnum.ModuleInfo);
        ProductUserId userId = _eosLobbyOperator.LocalProductUserId;
        List<ModuleSettingData> moduleSettingDatas = new List<ModuleSettingData>();
        _gameStatusActiveSOData.Camera = _gameCamera;
        ModuleSettingData baseModuleSettingData = new ModuleSettingData();
        _moduleSOData.GenerateModuleSettingData(ref baseModuleSettingData);
        baseModuleSettingData.ModuleNumber = 0;
        foreach (ModuleVersionEnum versionKey in baseModuleSettingData.ModuleGroup.Keys.ToList().Shuffle())
        {
            baseModuleSettingData.ModuleGroup[versionKey] = userId;
        }
        foreach (ModuleVersionEnum moduleVersion in _moduleSOData.ModulePrefabsDictionary.Keys)
        {
            moduleSettingDatas.Add(new ModuleSettingData()
            {
                ModuleNumber = 0,
                ModuleType = baseModuleSettingData.ModuleType,
                ModuleVersion = moduleVersion,
                ModuleGroup = baseModuleSettingData.ModuleGroup,
                ModuleData = baseModuleSettingData.ModuleData
            });
        }

        foreach (ModuleSettingData moduleSettingData in moduleSettingDatas.Shuffle())
        {
            GameObject module = Instantiate(
                _moduleSOData.ModulePrefabsDictionary[moduleSettingData.ModuleVersion],
                modulePlaceList[moduleSettingDatas.IndexOf(moduleSettingData)],
                Quaternion.identity,
                modulePlaceParent
            );
            module.GetComponentInChildren<ModuleInitializeOrchestrator>().Initialize(userId, new List<ProductUserId>() { userId }, new Dictionary<ProductUserId, Color>() { { userId, Color.red } }, moduleSettingData).Forget();
        }
    }
}