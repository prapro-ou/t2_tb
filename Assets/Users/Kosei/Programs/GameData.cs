using UnityEngine;

public class GameData : MonoBehaviour
{
    [Header("Game Settings")]
    public float minTargetTime = 2.0f;
    public float maxTargetTime = 8.0f;
    public float tolerance = 0.25f;

    [Header("Question")]
    public int questionCount = 2;

    // 各問題の目標時間
    public float[] targetTimes;

    // 現在の問題番号
    public int currentQuestion = 0;

    // ゲーム状態
    public bool gameClear = false;
    public bool gameFailed = false;
    public bool waitingNextQuestion = false;

    /// <summary>
    /// ゲーム開始時に問題を生成
    /// </summary>
    public void GenerateQuestions()
    {
        targetTimes = new float[questionCount];

        for (int i = 0; i < questionCount; i++)
        {
            targetTimes[i] =
                Random.Range(minTargetTime, maxTargetTime);
        }

        currentQuestion = 0;
        gameClear = false;
        gameFailed = false;
        waitingNextQuestion = false;
    }

    /// <summary>
    /// 現在の目標時間
    /// </summary>
    public float GetCurrentTarget()
    {
        return targetTimes[currentQuestion];
    }

    /// <summary>
    /// 次の問題へ
    /// </summary>
    public bool NextQuestion()
    {
        currentQuestion++;

        if (currentQuestion >= questionCount)
        {
            gameClear = true;
            return false;
        }

        return true;
    }
}