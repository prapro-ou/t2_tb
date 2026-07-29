using UnityEngine;
using TMPro;
using System.Collections;

public class OperatorGameManager : MonoBehaviour
{
    [Header("Manager")]
    public GameData gameData;
    public TimerGameManager timerGameManager;
    public ActionButtonController actionButton;

    [Header("UI")]
    public TMP_Text operatorInstructionText;
    public TMP_Text timerInstructionText;
    private bool timerRunning = false;

    private void Start()
    {
        StartGame();
    }

    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        gameData.GenerateQuestions();

        timerRunning = false;

        actionButton.SetPlay();

        ShowCurrentQuestion();
    }

    /// <summary>
    /// 現在の問題を表示
    /// </summary>
    private void ShowCurrentQuestion()
    {
        float target = gameData.GetCurrentTarget();

        string message =
        $"STOP\n" +
        $"{target - gameData.tolerance:F2} ～ {target + gameData.tolerance:F2}";

        operatorInstructionText.text = message;
        timerInstructionText.text = message;
    }

    /// <summary>
    /// アクションボタン
    /// </summary>
    public void PressActionButton()
    {
        if (gameData.gameClear)
            return;

        if (gameData.gameFailed)
            return;

        if (gameData.waitingNextQuestion)
            return;

        if (!timerRunning)
        {
            StartRound();
        }
        else
        {
            StopRound();
        }
    }

    /// <summary>
    /// ラウンド開始
    /// </summary>
    private void StartRound()
    {
        timerRunning = true;

        // タイマー役へ開始指示
        timerGameManager.ReceiveStart();

        // ボタンをSTOP表示へ
        actionButton.SetStop();
    }
        /// <summary>
    /// ラウンド終了
    /// </summary>
    private void StopRound()
    {
        timerRunning = false;

        // タイマー役へ停止指示
        timerGameManager.ReceiveStop();

        // 計測時間取得
        float time = timerGameManager.GetMeasuredTime();

        float target = gameData.GetCurrentTarget();

        bool success =
            time >= target - gameData.tolerance &&
            time <= target + gameData.tolerance;

        // ボタンをPLAY表示へ
        actionButton.SetPlay();

        if (success)
        {
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            operatorInstructionText.text = "MODULE FAILED!";
            timerInstructionText.text = "MODULE FAILED!";

            actionButton.SetFailed();

            gameData.gameFailed = true;
        }
    }

    /// <summary>
    /// SUCCESS表示
    /// </summary>
    private IEnumerator SuccessRoutine()
    {
        gameData.waitingNextQuestion = true;

        operatorInstructionText.text = "SUCCESS!";
        timerInstructionText.text = "SUCCESS!";

        actionButton.SetSuccess();

        yield return new WaitForSeconds(1.0f);

        bool hasNext = gameData.NextQuestion();

        if (!hasNext)
        {
            operatorInstructionText.text = "MODULE CLEAR!";
            timerInstructionText.text = "MODULE CLEAR!";

            actionButton.SetClear();

            yield break;
        }

        // 次の問題へ
        timerGameManager.ResetTimer();

        actionButton.SetPlay();

        ShowCurrentQuestion();

        gameData.waitingNextQuestion = false;
    }
}