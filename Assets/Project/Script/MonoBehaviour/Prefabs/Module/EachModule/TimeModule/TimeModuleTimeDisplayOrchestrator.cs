using UnityEngine;
using TMPro;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class TimeModuleTimeDisplayOrchestrator : MonoBehaviour
{
    [SerializeField] private TMP_Text _timeText;

    public void Initialize(TimeModuleSettingData moduleSettingData)
    {
        EOSP2PMethod.RegisterListener<TimeModuleTimeChangePacket>(TimeDisplay);
    }

    /// <summary>
    /// 時間変更
    /// </summary>
    /// <param name="moduleVersion"></param>
    /// <param name="packet"></param>
    private void TimeDisplay(ProductUserId remoteUserId, string socketName, TimeModuleTimeChangePacket packet)
    {
        float timeCount = packet.TimeCount;
        int minutes = Mathf.FloorToInt(timeCount / 60);
        int seconds = Mathf.FloorToInt(timeCount % 60);
        int milliseconds = Mathf.FloorToInt((timeCount * 100) % 100);
        if (minutes > 0)
        {
            _timeText.text = string.Format($"{minutes:00}:{seconds:00}");
        }
        else
        {
            _timeText.text = string.Format($"{seconds:00}.{milliseconds:00}");
        }
    }
}
