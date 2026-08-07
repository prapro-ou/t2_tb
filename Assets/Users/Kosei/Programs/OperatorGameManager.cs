using UnityEngine;
using TMPro;
using System.Collections;

public class OperatorGameManager : MonoBehaviour
{
    [Header("Boards")]
    public BoardController operatorInstructionBoard;
    public BoardController timerInstructionBoard;
    public BoardController timerBoard;

    [Header("Manager")]
    public GameData gameData;
    public TimerGameManager timerGameManager;
    public ActionButtonController actionButton;

    [Header("UI")]
    public TMP_Text operatorInstructionText;
    public TMP_Text timerInstructionText;

    private bool timerRunning = false;
    private bool inputLocked = false;

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

        gameData.gameClear = false;
        gameData.gameFailed = false;
        gameData.waitingNextQuestion = false;

        timerRunning = false;
        inputLocked = false;

        actionButton.SetPlay();

        operatorInstructionBoard.SetNormal();
        timerInstructionBoard.SetNormal();
        timerBoard.SetNormal();

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
            $"{target - gameData.tolerance:F2} - {target + gameData.tolerance:F2}";

        operatorInstructionText.text = message;
        timerInstructionText.text = message;
    }

    /// <summary>
    /// アクションボタン
    /// </summary>
    public void PressActionButton()
    {
        if (inputLocked)
            return;

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
        inputLocked = true;

        timerRunning = true;

        timerGameManager.ReceiveStart();

        actionButton.SetStop();

        inputLocked = false;
    }

    /// <summary>
    /// ラウンド終了
    /// </summary>
    private void StopRound()
    {
        inputLocked = true;

        timerRunning = false;

        timerGameManager.ReceiveStop();

        float measuredTime = timerGameManager.GetMeasuredTime();

        actionButton.SetPlay();

        if (IsSuccess(measuredTime))
        {
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            operatorInstructionText.text = "MODULE FAILED!";
            timerInstructionText.text = "MODULE FAILED!";

            operatorInstructionBoard.SetFailed();
            timerInstructionBoard.SetFailed();
            timerBoard.SetFailed();

            actionButton.SetFailed();

            gameData.gameFailed = true;
        }

        inputLocked = false;
    }

    /// <summary>
    /// 判定
    /// </summary>
    private bool IsSuccess(float measuredTime)
    {
        float target = gameData.GetCurrentTarget();

        return Mathf.Abs(measuredTime - target) <= gameData.tolerance;
    }

    /// <summary>
    /// SUCCESS表示
    /// </summary>
    private IEnumerator SuccessRoutine()
    {
        gameData.waitingNextQuestion = true;

        operatorInstructionText.text = "SUCCESS!";
        timerInstructionText.text = "SUCCESS!";

        operatorInstructionBoard.SetSuccess();
        timerInstructionBoard.SetSuccess();
        timerBoard.SetSuccess();

        actionButton.SetSuccess();

        yield return new WaitForSeconds(1.0f);

        bool hasNext = gameData.NextQuestion();

        if (!hasNext)
        {
            gameData.gameClear = true;

            operatorInstructionText.text = "MODULE CLEAR!";
            timerInstructionText.text = "MODULE CLEAR!";

            operatorInstructionBoard.SetClear();
            timerInstructionBoard.SetClear();
            timerBoard.SetClear();

            actionButton.SetClear();

            yield break;
        }

        timerGameManager.ResetTimer();

        operatorInstructionBoard.SetNormal();
        timerInstructionBoard.SetNormal();
        timerBoard.SetNormal();

        actionButton.SetPlay();

        ShowCurrentQuestion();

        gameData.waitingNextQuestion = false;
    }
}