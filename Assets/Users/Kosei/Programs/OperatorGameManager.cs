using UnityEngine;
using TMPro;

public class OperatorGameManager : MonoBehaviour
{
    [Header("Boards")]
    public BoardController operatorInstructionBoard;

    [Header("Manager")]
    public GameData gameData;
    public StopwatchModuleToolsOrchestrator stopwatchTools;
    public ActionButtonController actionButton;

    [Header("UI")]
    public TMP_Text operatorInstructionText;

    // 自分が操作側として担当しているバージョン
    private ModuleVersionEnum myModuleVersion;

    // 相手側のバージョン
    private ModuleVersionEnum targetModuleVersion;

    // 現在START中か
    private bool timerRunning = false;

    // 入力受付をロックする
    private bool inputLocked = false;

    // Time受信ListenerのUUID
    private string timeListenerUUID;

    private void Start()
    {
        Debug.Log("=== OperatorGameManager Start ===");
        Debug.Log($"stopwatchTools = {stopwatchTools}");
        Debug.Log($"gameData = {gameData}");
        Debug.Log($"actionButton = {actionButton}");

        StartGame();
    }

    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        Debug.Log("=== StartGame ===");
        Debug.Log($"stopwatchTools = {stopwatchTools}");

        if (stopwatchTools != null)
        {
            Debug.Log($"IsInitialize = {stopwatchTools.IsInitialize}");
        }
        else
        {
            Debug.LogError("stopwatchTools が null です。");
        }

        gameData.GenerateQuestions();

        gameData.gameClear = false;
        gameData.gameFailed = false;
        gameData.waitingNextQuestion = false;

        timerRunning = false;
        inputLocked = false;

        // 自分のモジュールバージョンを取得
        myModuleVersion =
            stopwatchTools.GetMyModuleVersion();

        Debug.Log($"myModuleVersion = {myModuleVersion}");

        // AならB、BならAを相手にする
        targetModuleVersion =
            myModuleVersion == ModuleVersionEnum.A
                ? ModuleVersionEnum.B
                : ModuleVersionEnum.A;

        Debug.Log($"targetModuleVersion = {targetModuleVersion}");

        // 計測時間の受信登録
        timeListenerUUID =
            stopwatchTools.RegisterTimeListener(OnTimeReceived);

        Debug.Log($"timeListenerUUID = {timeListenerUUID}");

        actionButton.SetPlay();

        operatorInstructionBoard.SetNormal();

        ShowCurrentQuestion();

        Debug.Log("=== StartGame 完了 ===");
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
    }

    /// <summary>
    /// アクションボタン
    /// </summary>
    public void PressActionButton()
    {
        Debug.Log("=== PressActionButton ===");

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
        Debug.Log("=== StartRound ===");
        Debug.Log($"stopwatchTools = {stopwatchTools}");
        Debug.Log($"myModuleVersion = {myModuleVersion}");
        Debug.Log($"targetModuleVersion = {targetModuleVersion}");

        inputLocked = true;
        timerRunning = true;

        Debug.Log("SendAction 前");

        // 指示側へSTARTを送信
        stopwatchTools.SendAction(
            targetModuleVersion,
            true
        );

        Debug.Log("SendAction 後");

        actionButton.SetStop();

        inputLocked = false;
    }

    /// <summary>
    /// ラウンド終了
    /// </summary>
    private void StopRound()
    {
        Debug.Log("=== StopRound ===");
        Debug.Log($"targetModuleVersion = {targetModuleVersion}");

        inputLocked = true;
        timerRunning = false;

        Debug.Log("SendAction(STOP) 前");

        // 指示側へSTOPを送信
        stopwatchTools.SendAction(
            targetModuleVersion,
            false
        );

        Debug.Log("SendAction(STOP) 後");

        actionButton.SetPlay();

        // ここではまだ判定しない
        // 指示側から計測時間が送られてくるのを待つ
    }

    /// <summary>
    /// 指示側から計測時間を受信
    /// </summary>
    private void OnTimeReceived(
        ModuleVersionEnum moduleVersion,
        float measuredTime)
    {
        Debug.Log("=== OnTimeReceived ===");
        Debug.Log($"moduleVersion = {moduleVersion}");
        Debug.Log($"targetModuleVersion = {targetModuleVersion}");
        Debug.Log($"measuredTime = {measuredTime}");

        // 自分が送った相手からの時間だけ処理
        if (moduleVersion != targetModuleVersion)
            return;

        Debug.Log(
            $"Received Time: {measuredTime:F2}"
        );

        if (IsSuccess(measuredTime))
        {
            SuccessRound();
        }
        else
        {
            FailedRound();
        }
    }

    /// <summary>
    /// 計測時間の判定
    /// </summary>
    private bool IsSuccess(float measuredTime)
    {
        float target =
            gameData.GetCurrentTarget();

        return Mathf.Abs(
            measuredTime - target
        ) <= gameData.tolerance;
    }

    /// <summary>
    /// 成功処理
    /// </summary>
    private void SuccessRound()
    {
        gameData.waitingNextQuestion = true;

        operatorInstructionText.text =
            "SUCCESS!";

        operatorInstructionBoard.SetSuccess();

        actionButton.SetSuccess();

        Invoke(
            nameof(NextQuestion),
            1.0f
        );
    }

    /// <summary>
    /// 失敗処理
    /// </summary>
    private void FailedRound()
    {
        operatorInstructionText.text =
            "MODULE FAILED!";

        operatorInstructionBoard.SetFailed();

        actionButton.SetFailed();

        gameData.gameFailed = true;

        inputLocked = false;
    }

    /// <summary>
    /// 次の問題へ
    /// </summary>
    private void NextQuestion()
    {
        bool hasNext =
            gameData.NextQuestion();

        if (!hasNext)
        {
            gameData.gameClear = true;

            operatorInstructionText.text =
                "MODULE CLEAR!";

            operatorInstructionBoard.SetClear();

            actionButton.SetClear();

            return;
        }

        operatorInstructionBoard.SetNormal();

        actionButton.SetPlay();

        ShowCurrentQuestion();

        gameData.waitingNextQuestion = false;
        inputLocked = false;
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(timeListenerUUID))
        {
            stopwatchTools.ModuleDataUnregisterListener(
                timeListenerUUID
            );
        }
    }
}