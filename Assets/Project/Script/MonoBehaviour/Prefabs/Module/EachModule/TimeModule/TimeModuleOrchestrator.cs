using UnityEngine;
public class TimeModuleOrchestrator : MonoBehaviour
{
    [SerializeField] private TimeModuleTimeDisplayMediator _timeModuleTimeDisplayMediator;
    public void Initialize(TimeModuleSettingData moduleSettingData)
    {
        _timeModuleTimeDisplayMediator.Initialize(moduleSettingData);
    }
}