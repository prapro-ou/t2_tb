using UnityEngine;
public class AllModuleSOData : ScriptableObject
{
    [SerializeField] private SerializedDictionary<ModuleTypeEnum, ModuleSOData> _moduleDatas = new SerializedDictionary<ModuleTypeEnum, ModuleSOData>();
    public SerializedDictionary<ModuleTypeEnum, ModuleSOData> ModuleDatas => _moduleDatas;
}