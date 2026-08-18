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

    [Header("UI")]
    public TMP_Text operatorInstructionText;

    // 現在START中か
    private bool timerRunning = false;

    // 入力受付をロックする
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

    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        Debug.Log("=== StartGame ===");

        // 必要なコンポーネント確認
        if (gameData == null)
        {
            Debug.LogError(
                "GameData が設定されていません。"
            );
            return;
        }

        if (timerGameManager == null)
        {
            Debug.LogError(
                "TimerGameManager が設定されていません。"
            );
            return;
        }

        if (actionButton == null)
        {
            Debug.LogError(
                "ActionButtonController が設定されていません。"
            );
            return;
        }

        if (operatorInstructionBoard == null)
        {
            Debug.LogError(
                "OperatorInstructionBoard が設定されていません。"
            );
            return;
        }

        if (operatorInstructionText == null)
        {
            Debug.LogError(
                "OperatorInstructionText が設定されていません。"
            );
            return;
        }

        // 問題生成
        gameData.GenerateQuestions();

        // 状態初期化
        gameData.gameClear = false;
        gameData.gameFailed = false;
        gameData.waitingNextQuestion = false;

        timerRunning = false;
        inputLocked = false;

        // 操作側を初期状態にする
        actionButton.SetPlay();

        operatorInstructionBoard.SetNormal();

        // タイマー側も初期状態にする
        timerGameManager.ResetTimer();
        timerGameManager.SetNormal();

        // 現在の問題を表示
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
            $"PRESS BUTTON\n" +
            $"STOP\n" +
            $"{target - gameData.tolerance:F2} - {target + gameData.tolerance:F2}";

        operatorInstructionText.text = message;

        // タイマー側にも問題を表示
        timerGameManager.ShowQuestion(
            target,
            gameData.tolerance
        );
    }

    /// <summary>
    /// アクションボタン
    /// </summary>
    public void PressActionButton()
    {
        Debug.Log("=== PressActionButton ===");

        // 入力ロック中
        if (inputLocked)
        {
            Debug.Log("入力がロックされています。");
            return;
        }

        // ゲームクリア済み
        if (gameData.gameClear)
        {
            Debug.Log("ゲームはすでにクリアしています。");
            return;
        }

        // 次の問題への待機中
        if (gameData.waitingNextQuestion)
        {
            Debug.Log("次の問題への移行待ちです。");
            return;
        }

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

        inputLocked = true;
        timerRunning = true;

        // 操作側の表示を変更
        operatorInstructionText.text = "STOP";

        actionButton.SetStop();

        // タイマー側にSTARTを直接通知
        timerGameManager.ReceiveStart();

        inputLocked = false;

        Debug.Log("=== StartRound 完了 ===");
    }

    /// <summary>
    /// ラウンド終了
    /// </summary>
    private void StopRound()
    {
        Debug.Log("=== StopRound ===");

        inputLocked = true;
        timerRunning = false;

        // タイマー側にSTOPを直接通知
        timerGameManager.ReceiveStop();

        // 操作側のボタンをPLAY状態に戻す
        actionButton.SetPlay();

        // 停止した時間を取得
        float measuredTime =
            timerGameManager.GetMeasuredTime();

        Debug.Log(
            $"Measured Time = {measuredTime:F2}"
        );

        // 時間を判定
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

        float difference =
            Mathf.Abs(measuredTime - target);

        Debug.Log(
            $"Target = {target:F2}, " +
            $"Measured = {measuredTime:F2}, " +
            $"Difference = {difference:F2}, " +
            $"Tolerance = {gameData.tolerance:F2}"
        );

        return difference <= gameData.tolerance;
    }

    /// <summary>
    /// 成功処理
    /// </summary>
    private void SuccessRound()
    {
        Debug.Log("=== SuccessRound ===");

        // 次の問題への待機
        gameData.waitingNextQuestion = true;

        // 操作側
        operatorInstructionText.text =
            "SUCCESS!";

        operatorInstructionBoard.SetSuccess();

        actionButton.SetSuccess();

        // タイマー側
        timerGameManager.SetSuccess();

        // 1秒後に次の問題へ
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
        Debug.Log("=== FailedRound ===");

        // 操作側
        operatorInstructionText.text =
            "MODULE FAILED!";

        operatorInstructionBoard.SetFailed();

        actionButton.SetFailed();

        // タイマー側
        timerGameManager.SetFailed();

        // ゲームオーバーにはしない
        gameData.gameFailed = false;

        // 1秒後に同じ問題を再挑戦
        Invoke(
            nameof(RetryCurrentQuestion),
            1.0f
        );
    }

    /// <summary>
    /// 現在の問題を再挑戦
    /// </summary>
    private void RetryCurrentQuestion()
    {
        Debug.Log("=== RetryCurrentQuestion ===");

        // ゲームクリア済みなら何もしない
        if (gameData.gameClear)
            return;

        // タイマーをリセット
        timerGameManager.ResetTimer();

        // タイマー側を通常状態へ
        timerGameManager.SetNormal();

        // 操作側を通常状態へ
        operatorInstructionBoard.SetNormal();

        // 同じ問題を再表示
        ShowCurrentQuestion();

        // ボタンをPLAY状態へ
        actionButton.SetPlay();

        // 状態を初期化
        timerRunning = false;
        inputLocked = false;

        gameData.waitingNextQuestion = false;

        Debug.Log(
            $"問題 {gameData.currentQuestion + 1} を再挑戦します。"
        );
    }

    /// <summary>
    /// 次の問題へ
    /// </summary>
    private void NextQuestion()
    {
        Debug.Log("=== NextQuestion ===");

        bool hasNext =
            gameData.NextQuestion();

        // 指定問題数すべて成功
        if (!hasNext)
        {
            gameData.gameClear = true;
            gameData.waitingNextQuestion = false;

            // 操作側
            operatorInstructionText.text =
                "MODULE CLEAR!";

            operatorInstructionBoard.SetClear();

            actionButton.SetClear();

            // タイマー側
            timerGameManager.SetClear();

            Debug.Log("=== MODULE CLEAR ===");

            return;
        }

        // まだ次の問題がある
        operatorInstructionBoard.SetNormal();

        actionButton.SetPlay();

        // 次の問題を表示
        ShowCurrentQuestion();

        gameData.waitingNextQuestion = false;
        inputLocked = false;
        timerRunning = false;

        Debug.Log(
            $"次の問題へ: " +
            $"{gameData.currentQuestion + 1} / " +
            $"{gameData.questionCount}"
        );
    }

    private void OnDestroy()
    {
        // 現在は通信を使用していないため、
        // 通信Listenerの解除処理はありません。
    }
}