using System.Linq;
using UnityEngine;
using Epic.OnlineServices;
[CreateAssetMenu(fileName = "ModuleSOData", menuName = "SO/ModuleSOData")]
public class ModuleSOData : ScriptableObject
{
    [SerializeField] private ModuleTypeEnum _moduleType;
    public ModuleTypeEnum ModuleType => _moduleType;
    [SerializeField] private SerializedDictionary<ModuleVersionEnum, GameObject> _modulePrefabsDictionary = new SerializedDictionary<ModuleVersionEnum, GameObject>();
    public SerializedDictionary<ModuleVersionEnum, GameObject> ModulePrefabsDictionary => _modulePrefabsDictionary;
    [SerializeField] private ModuleSettingDataGenerateSOMethod _dataGanerate;

    public ModuleSettingData GenerateModuleSettingData()
    {
        // 0.初期確認
        return new ModuleSettingData
        {
            // 1.モジュールタイプの確認
            ModuleType = _moduleType,

            // 2.モジュールバージョンの確認
            ModuleGroup = _modulePrefabsDictionary.Keys.ToDictionary(
                key => key,
                key => (ProductUserId)null
            ),

            // 3.モジュールデータの取得
            ModuleData = _dataGanerate.Generate()
        };
    }
}