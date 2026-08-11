using System;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
public class TimeModuleTimeDisplayMediator : MonoBehaviour
{
    [SerializeField] private TMP_Text _timeText;

    public void Initialize(TimeModuleSettingData moduleSettingData)
    {
        Debug.Log(moduleSettingData.Time);
        SettingTime(moduleSettingData.Time).Forget();
    }

    /// <summary>
    /// 時間表示を指定した時間かけてカウントアップ演出する
    /// </summary>
    /// <param name="targetTime">目標時間</param>
    /// <param name="duration">アニメーションにかける時間（秒）</param>
    /// <param name="cancellationToken">キャンセル用トークン</param>
    private async UniTask SettingTime(TimeSpan targetTime, float duration = 5.0f)
    {
        double targetSeconds = targetTime.TotalSeconds;

        // 0以下の時間、または演出時間が0以下の場合は即座に最終値を表示して終了
        if (targetSeconds <= 0 || duration <= 0)
        {
            TimeDisplay(targetTime);
            return;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // 0.0 ～ 1.0 の進捗率を計算
            float progress = Mathf.Clamp01(elapsedTime / duration);

            // 必要に応じてイージング（例: EaseOutQuadratic の場合: progress * (2 - progress) など）
            double currentSeconds = targetSeconds * (1f - Mathf.Pow(1f - progress, 3f));

            TimeDisplay(TimeSpan.FromSeconds(currentSeconds));

            // キャンセルチェック付きで1フレーム待機
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        // 最終的に確実に目標値を表示する
        TimeDisplay(targetTime);
    }

    public void UpdateTime(TimeSpan timeSpan) => TimeDisplay(timeSpan);

    /// <summary>
    /// 時間変更
    /// </summary>
    /// <param name="moduleVersion"></param>
    /// <param name="packet"></param>
    private void TimeDisplay(TimeSpan _timeSpan)
    {
        int minutes = _timeSpan.Minutes;
        int seconds = _timeSpan.Seconds;
        int milliseconds = _timeSpan.Milliseconds;
        if (minutes > 0)
        {
            _timeText.text = $"<mspace=0.55em>{minutes:00}:{seconds:00}</mspace=0.55em>";
        }
        else
        {
            _timeText.text = $"<mspace=0.55em>{seconds:00}.{milliseconds / 10:00}</mspace=0.55em>";
        }
    }
}
