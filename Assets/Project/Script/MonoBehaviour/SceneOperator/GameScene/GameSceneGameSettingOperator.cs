using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;

public class GameSceneGameSettingOperator : MonoBehaviour
{
    [SerializeField] private AllModuleSOData _allModuleSOData;
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;
    [SerializeField] private Transform modulePlaceParent;
    [SerializeField] private List<Vector2> modulePlaceList;

    public async UniTask Initialize()
    {
        // 0.初期確認
        ProductUserId userId = _eosLobbyOperator.LocalProductUserId;
        List<ModuleSettingData> moduleSettingDatas = _gameStatusActiveSOData.ThisGameSettingPacket.ModuleSettingDatas[userId];
        List<UniTask> moduleInitializeTasks = new List<UniTask>();

        // 1.モジュール確認
        while (modulePlaceList.Count < 14)
        {
            moduleSettingDatas.Add(new ModuleSettingData()
            {
                ModuleType = ModuleTypeEnum.Empty,
                ModuleVersion = ModuleVersionEnum.A,
                ModuleGroup = null,
                ModuleData = null
            }
            );
        }

        // 2.モジュール配置
        // 2.0.初期確認
        List<Object> modules = new List<Object>();

        // 2.1.モジュール配置
        foreach (ModuleSettingData moduleSettingData in moduleSettingDatas)
        {

            GameObject module = Instantiate(
                _allModuleSOData.ModuleDatas[moduleSettingData.ModuleType].ModulePrefabsDictionary[moduleSettingData.ModuleVersion],
                modulePlaceList[0],
                Quaternion.identity,
                modulePlaceParent
            );

            modules.Add(module);
            module.GetComponentInChildren<ModuleInitializeOrchestrator>().Initialize(userId, _gameStatusActiveSOData.ThisGameSettingPacket.PlayerColors, moduleSettingData);
        }




        // 3.ゲーム準備完了
        await UniTask.WhenAll(moduleInitializeTasks);
        EOSP2PMethod.SendPacket(SocketNameEnum.Fallback, _gameStatusActiveSOData.ThisGameSettingPacket.HostUserId, new GameSettingEndSignalPacket() { IsSetting = true });
    }

}