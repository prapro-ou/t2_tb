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
    [SerializeField] private AllModuleSOData _allModuleSOData;
    [SerializeField] private ProjectOverseer _gameOverseer;
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;
    [SerializeField] private int _moduleTypeCount;

    public void OnClick()
    {
        Debug.Log("StartGame");
        HostStartGame(_moduleTypeCount);
    }

    /// <summary>
    /// ホストがゲームを開始する
    /// </summary>
    /// <param name="moduleTypeCount"></param>
    public void HostStartGame(int moduleTypeCount)
    {
        if (_gameStatusActiveSOData.IsStartGameSetting) return;
        // 0.初期確認
        GameSettingPacket packet = new GameSettingPacket(); // 送信パケット
        packet.HostUserId = EOSManager.Instance.GetProductUserId();
        List<ModuleSettingData> moduleSettingDatas = new List<ModuleSettingData>(); // 送信モジュールデータ
        List<ProductUserId> members = EOSLobbyMethod.GetLobbyMembers(_eosLobbyOperator.CurrentLobbyId); // ロビーメンバー
        int panelCount = 0; // パネル数
        List<ProductUserId> choiseMembers = new List<ProductUserId>(); // 選択メンバー
        if (moduleTypeCount < members.Count && 8 * members.Count < moduleTypeCount)
        {
            return;
        }

        // 2.送信モジュールデータ構築
        // 2.1.モジュール選択
        List<ModuleTypeEnum> moduleTypes = EnumExtensions.GetUniqueEnumValues<ModuleTypeEnum>(moduleTypeCount);
        // 2.2.モジュール情報構築
        foreach (ModuleTypeEnum moduleType in moduleTypes)
        {
            ModuleSettingData moduleSettingData = _allModuleSOData.ModuleDatas[moduleType].GenerateModuleSettingData();
            panelCount += moduleSettingData.ModuleGroup.Count;
            moduleSettingDatas.Add(moduleSettingData);
        }

        // 3.メンバー順番決め
        System.Random rand = new System.Random();
        choiseMembers = Enumerable.Range(0, panelCount)
            .Select(i => members[i % members.Count]) // 均等にメンバーを抽出
            .OrderBy(_ => rand.Next())               // ランダムに並び替え
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
            new Color(255,  59,  48, 255), // 1P: 赤 (Red)
            new Color(  0, 122, 255, 255), // 2P: 青 (Blue)
            new Color(255, 204,   0, 255), // 3P: 黄 (Yellow)
            new Color( 52, 199,  89, 255), // 4P: 緑 (Green)
            new Color(175,  82, 222, 255), // 5P: 紫 (Purple)
            new Color(255, 149,   0, 255), // 6P: 橙 (Orange)
            new Color( 90, 200, 250, 255), // 7P: 水 (Cyan)
            new Color(255,  45,  85, 255)  // 8P: 桃 (Pink)
        };
        colors.Shuffle();
        Dictionary<ProductUserId, Color> playerColors = new Dictionary<ProductUserId, Color>();
        for (int i = 0; i < members.Count; i++)
        {
            playerColors[members[i]] = colors[i];
        }
        packet.PlayerColors = playerColors;

        // 5.データ構築
        for (int i = 0; i < moduleSettingDatas.Count; i++)
        {
            ModuleSettingData originalData = moduleSettingDatas[i]; // 元データ
            if (originalData.ModuleGroup == null) continue;

            // 5.1.担当者情報
            foreach (ModuleVersionEnum versionKey in originalData.ModuleGroup.Keys.ToArray())
            {
                ProductUserId ownerId = queueMembers.Dequeue();
                originalData.ModuleGroup[versionKey] = ownerId;
            }

            // 5.2.モジュール情報分配
            foreach (var kvp in originalData.ModuleGroup)
            {
                ModuleVersionEnum versionKey = kvp.Key;
                ProductUserId ownerId = kvp.Value;
                ModuleSettingData playerModuleData = new ModuleSettingData
                {
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
        _gameStatusActiveSOData.GameSettingEndRegister(members);
        foreach (ProductUserId userId in members)
        {
            EOSP2PMethod.SendPacket(SocketNameEnum.Fallback, userId, packet);
        }
    }

}