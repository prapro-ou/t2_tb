using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Manager")]
    public TimerManager timerManager;
    public CoverController coverController;


    [Header("UI")]
    public TMP_Text instructionText1;

    public Button startButton;
    public Button stopButton;


    [Header("Game Settings")]
    public float minTargetTime = 2.0f;
    public float maxTargetTime = 8.0f;
    public float tolerance = 0.25f;


    [Header("Cover")]
    public float coverDelay = 4.0f;


    // 問題数
    private const int QUESTION_COUNT = 2;


    // 目標時間
    private float[] targetTimes = new float[QUESTION_COUNT];


    // 現在の問題番号
    private int currentQuestion = 0;


    // 状態管理
    private bool gameClear = false;
    private bool gameFailed = false;
    private bool timerRunning = false;
    private bool waitingNextQuestion = false;



    void Start()
    {
        StartGame();
    }



    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        gameClear = false;
        gameFailed = false;

        currentQuestion = 0;
        timerRunning = false;
        waitingNextQuestion = false;


        // 問題生成
        for (int i = 0; i < QUESTION_COUNT; i++)
        {
            targetTimes[i] =
                Random.Range(minTargetTime, maxTargetTime);
        }

        if (coverController != null)
        {
            coverController.OpenInstant();
        }

        UpdateButtonState();

        ShowCurrentQuestion();
    }



    /// <summary>
    /// 問題表示
    /// </summary>
    private void ShowCurrentQuestion()
    {
        float target = targetTimes[currentQuestion];

        instructionText1.text =
            $"STOP\n" +
            $"{target - tolerance:F2} - {target + tolerance:F2}";
    }




    /// <summary>
    /// STARTボタン
    /// </summary>
    public void PressStartButton()
    {
        if (waitingNextQuestion)
            return;

        if (gameClear || gameFailed)
            return;

        if (timerRunning)
            return;

        timerRunning = true;

        timerManager.StartTimer();

        if (coverController != null)
        {
            coverController.OpenInstant();
            StartCoroutine(CloseCoverAfterDelay());
        }

        UpdateButtonState();
    }




    /// <summary>
    /// STOPボタン
    /// </summary>
    public void PressStopButton()
    {
        if (waitingNextQuestion)
            return;

        if (gameClear || gameFailed)
            return;

        if (!timerRunning)
            return;

        timerManager.StopTimer();

        if (coverController != null)
        {
            coverController.Open();
        }

        timerRunning = false;

        float time =
            timerManager.GetCurrentTime();

        float target =
            targetTimes[currentQuestion];

        bool success =
            time >= target - tolerance &&
            time <= target + tolerance;

        if (success)
        {
            currentQuestion++;

            // 全問クリア
            if (currentQuestion >= QUESTION_COUNT)
            {
                instructionText1.text =
                    "MODULE CLEAR!";

                gameClear = true;
            }
            else
            {
                StartCoroutine(
                    ShowSuccessAndNextQuestion());
            }
        }
        else
        {
            instructionText1.text =
                "MODULE FAILED!";

            gameFailed = true;
        }

        UpdateButtonState();
    }




    /// <summary>
    /// SUCCESS表示
    /// </summary>
    private IEnumerator ShowSuccessAndNextQuestion()
    {
        waitingNextQuestion = true;

        instructionText1.text =
            "SUCCESS!";

        UpdateButtonState();

        yield return new WaitForSeconds(1.0f);

        waitingNextQuestion = false;

        ShowCurrentQuestion();

        UpdateButtonState();
    }




    /// <summary>
    /// 指定時間後にカバーを閉じる
    /// </summary>
    private IEnumerator CloseCoverAfterDelay()
    {
        yield return new WaitForSeconds(coverDelay);

        if (timerRunning && coverController != null)
        {
            coverController.Close();
        }
    }




    /// <summary>
    /// ボタン状態変更
    /// </summary>
    private void UpdateButtonState()
    {
        if (startButton == null || stopButton == null)
            return;

        // ゲーム終了
        if (gameClear || gameFailed)
        {
            startButton.interactable = false;
            stopButton.interactable = false;
            return;
        }

        // タイマー中
        if (timerRunning)
        {
            startButton.interactable = false;
            stopButton.interactable = true;
        }
        // 待機中
        else
        {
            startButton.interactable = true;
            stopButton.interactable = false;
        }
    }
}