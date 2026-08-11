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

    public void GenerateModuleSettingData(ref ModuleSettingData moduleSettingData)
    {
        moduleSettingData.ModuleType = _moduleType;
        moduleSettingData.ModuleGroup = _modulePrefabsDictionary.Keys.ToDictionary(
            key => key,
            key => (ProductUserId)null
        );
        moduleSettingData.ModuleData = _dataGanerate.Generate();
    }
}