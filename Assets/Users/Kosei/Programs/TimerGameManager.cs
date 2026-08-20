using UnityEngine;
using TMPro;

public class TimerGameManager : MonoBehaviour
{
    [Header("Manager")]
    public TimerManager timerManager;

    [Header("Board")]
    public BoardController timerInstructionBoard;

    [Header("UI")]
    public TMP_Text instructionText;

    /// <summary>
    /// 指示文を表示
    /// </summary>
    private void ShowInstruction(string message)
    {
        if (instructionText != null)
        {
            instructionText.text = message;
        }
        else
        {
            Debug.LogError("TimerGameManager: InstructionText が設定されていません。");
        }
    }

    /// <summary>
    /// 問題を表示（通常時は WATCH!!、再挑戦時は RETRY!）
    /// </summary>
    public void ShowQuestion(float target, float tolerance, bool isRetry = false)
    {
        float min = target - tolerance;
        float max = target + tolerance;

        string header = isRetry ? "RETRY!" : "WATCH!!";
        ShowInstruction($"{header}\n{min:F2} - {max:F2}");

        // 通常色に戻す（緑・赤枠を解除）
        SetNormal();
    }

    /// <summary>
    /// STARTを受信
    /// </summary>
    public void ReceiveStart()
    {
        Debug.Log("=== TimerGameManager ReceiveStart ===");

        if (timerManager == null)
        {
            Debug.LogError("TimerManager が設定されていません。");
            ShowInstruction("TIMER ERROR");
            return;
        }

        timerManager.ResetTimer();
        timerManager.StartTimer();
    }

    /// <summary>
    /// STOPを受信
    /// </summary>
    public void ReceiveStop()
    {
        Debug.Log("=== TimerGameManager ReceiveStop ===");

        if (timerManager == null)
        {
            Debug.LogError("TimerManager が設定されていません。");
            ShowInstruction("TIMER ERROR");
            return;
        }

        timerManager.StopTimer();
    }

    /// <summary>
    /// 成功
    /// </summary>
    public void SetSuccess()
    {
        ShowInstruction("SUCCESS!");

        if (timerInstructionBoard != null)
        {
            timerInstructionBoard.SetSuccess();
        }
    }

    /// <summary>
    /// 失敗
    /// </summary>
    public void SetFailed()
    {
        ShowInstruction("FAILED!");

        if (timerInstructionBoard != null)
        {
            timerInstructionBoard.SetFailed();
        }
    }

    /// <summary>
    /// クリア
    /// </summary>
    public void SetClear()
    {
        ShowInstruction("MODULE CLEAR!");

        if (timerInstructionBoard != null)
        {
            timerInstructionBoard.SetClear();
        }
    }

    /// <summary>
    /// 通常状態
    /// </summary>
    public void SetNormal()
    {
        if (timerInstructionBoard != null)
        {
            timerInstructionBoard.SetNormal();
        }
    }

    /// <summary>
    /// 現在の計測時間を取得
    /// </summary>
    public float GetMeasuredTime()
    {
        if (timerManager == null)
        {
            Debug.LogError("TimerManager が設定されていません。");
            return 0f;
        }

        return timerManager.GetCurrentTime();
    }

    /// <summary>
    /// タイマーをリセット
    /// </summary>
    public void ResetTimer()
    {
        if (timerManager == null)
        {
            Debug.LogError("TimerManager が設定されていません。");
            return;
        }

        timerManager.ResetTimer();
    }
}