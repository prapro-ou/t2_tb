using UnityEngine;
using TMPro;

public class OperatorGameManager : MonoBehaviour
{
    [Header("Boards")]
    public BoardController operatorInstructionBoard;

    [Header("Manager")]
    public GameData gameData;
    public TimerGameManager timerGameManager;
    public ActionButtonController actionButton;
    public StopwatchPlayerToolsOrchestrator stopwatchTools;

    [Header("UI")]
    public TMP_Text operatorInstructionText;

    private bool timerRunning = false;
    private bool inputLocked = false;

    private void Start()
    {
        Debug.Log("=== OperatorGameManager Start ===");

        Debug.Log($"gameData = {gameData}");
        Debug.Log($"timerGameManager = {timerGameManager}");
        Debug.Log($"actionButton = {actionButton}");
        Debug.Log($"operatorInstructionBoard = {operatorInstructionBoard}");
        Debug.Log($"operatorInstructionText = {operatorInstructionText}");

        StartGame();
    }

    public void StartGame()
    {
        Debug.Log("=== StartGame ===");

        if (gameData == null || actionButton == null || operatorInstructionBoard == null || operatorInstructionText == null)
        {
            Debug.LogError("コンポーネントの設定が不足しています。");
            return;
        }

        gameData.GenerateQuestions();

        gameData.gameClear = false;
        gameData.gameFailed = false;
        gameData.waitingNextQuestion = false;

        timerRunning = false;
        inputLocked = false;

        actionButton.SetPlay();
        operatorInstructionBoard.SetNormal();

        // 初期表示（isRetry = false）
        ShowCurrentQuestion(false);

        Debug.Log("=== StartGame 完了 ===");
    }

    /// <summary>
    /// 現在の問題を表示（指示側にも連動通知）
    /// </summary>
    private void ShowCurrentQuestion(bool isRetry = false)
    {
        float target = gameData.GetCurrentTarget();
        float tolerance = gameData.tolerance;

        float minTime = target - tolerance;
        float maxTime = target + tolerance;

        // 操作側UI
        string message = $"PRESS BUTTON\n{minTime:F2} ~ {maxTime:F2}s";
        operatorInstructionText.text = message;

        // 指示側UI（WATCH!! または RETRY!）と色のリセット
        if (timerGameManager != null)
        {
            timerGameManager.ShowQuestion(target, tolerance, isRetry);
        }
    }

    public void PressActionButton()
    {
        Debug.Log("=== PressActionButton ===");

        if (inputLocked || gameData.gameClear || gameData.waitingNextQuestion)
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

    private void StartRound()
    {
        Debug.Log("=== StartRound ===");

        inputLocked = true;
        timerRunning = true;

        operatorInstructionText.text = "STOP";
        actionButton.SetStop();

        if (stopwatchTools != null)
        {
            stopwatchTools.SendStart();
        }

        // 単体テスト用連動
        if (timerGameManager != null)
        {
            timerGameManager.ReceiveStart();
        }

        inputLocked = false;
    }

    private void StopRound()
    {
        Debug.Log("=== StopRound ===");

        inputLocked = true;
        timerRunning = false;

        if (stopwatchTools != null)
        {
            stopwatchTools.SendStop();
        }

        // 単体テスト用連動
        if (timerGameManager != null)
        {
            timerGameManager.ReceiveStop();
        }

        actionButton.SetPlay();

        float measuredTime = (timerGameManager != null) ? timerGameManager.GetMeasuredTime() : 0f;

        if (IsSuccess(measuredTime))
        {
            SuccessRound();
        }
        else
        {
            FailedRound();
        }
    }

    private bool IsSuccess(float measuredTime)
    {
        float target = gameData.GetCurrentTarget();
        float difference = Mathf.Abs(measuredTime - target);
        return difference <= gameData.tolerance;
    }

    private void SuccessRound()
    {
        Debug.Log("=== SuccessRound ===");

        gameData.waitingNextQuestion = true;

        operatorInstructionText.text = "SUCCESS!";
        operatorInstructionBoard.SetSuccess();
        actionButton.SetSuccess();

        if (timerGameManager != null)
        {
            timerGameManager.SetSuccess();
        }

        Invoke(nameof(NextQuestion), 1.0f);
    }

    private void FailedRound()
    {
        Debug.Log("=== FailedRound ===");

        operatorInstructionText.text = "MODULE FAILED!";
        operatorInstructionBoard.SetFailed();
        actionButton.SetFailed();

        if (timerGameManager != null)
        {
            timerGameManager.SetFailed();
        }

        gameData.gameFailed = false;

        Invoke(nameof(RetryCurrentQuestion), 1.0f);
    }

    private void RetryCurrentQuestion()
    {
        Debug.Log("=== RetryCurrentQuestion ===");

        if (gameData.gameClear) return;

        if (timerGameManager != null)
        {
            timerGameManager.ResetTimer();
        }

        operatorInstructionBoard.SetNormal();

        // 指示側に RETRY! と表示し、色を通常に戻す
        ShowCurrentQuestion(true);

        actionButton.SetPlay();

        timerRunning = false;
        inputLocked = false;
        gameData.waitingNextQuestion = false;
    }

    private void NextQuestion()
    {
        Debug.Log("=== NextQuestion ===");

        bool hasNext = gameData.NextQuestion();

        if (!hasNext)
        {
            gameData.gameClear = true;
            gameData.waitingNextQuestion = false;

            operatorInstructionText.text = "MODULE CLEAR!";
            operatorInstructionBoard.SetClear();
            actionButton.SetClear();

            if (timerGameManager != null)
            {
                timerGameManager.SetClear();
            }

            return;
        }

        operatorInstructionBoard.SetNormal();
        actionButton.SetPlay();

        // 指示側に WATCH!! と表示し、色を通常に戻す
        ShowCurrentQuestion(false);

        gameData.waitingNextQuestion = false;
        inputLocked = false;
        timerRunning = false;
    }
}