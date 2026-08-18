using UnityEngine;

public class GameData : MonoBehaviour
{
    [Header("Game Settings")]
    public float minTargetTime = 2.0f;
    public float maxTargetTime = 8.0f;
    public float tolerance = 0.25f;

    [Header("Question")]
    public int questionCount = 3;

    public float[] targetTimes;

    public int currentQuestion = 0;

    public bool gameClear = false;
    public bool gameFailed = false;
    public bool waitingNextQuestion = false;

    /// <summary>
    /// SettingDataからゲーム設定を初期化
    /// </summary>
    public void Initialize(StopwatchModuleSettingData data)
    {
        minTargetTime = data.minTargetTime;
        maxTargetTime = data.maxTargetTime;
        tolerance = data.tolerance;
        questionCount = data.questionCount;

        Debug.Log(
            $"Stopwatch Initialize: " +
            $"min={minTargetTime}, " +
            $"max={maxTargetTime}, " +
            $"tolerance={tolerance}, " +
            $"questionCount={questionCount}"
        );
    }

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

    public float GetCurrentTarget()
    {
        return targetTimes[currentQuestion];
    }

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