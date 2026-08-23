using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public float limitTime = 60f; // 制限時間60秒
    public TMP_Text timerText;

    private float currentTime;
    private bool isRunning = true;

    void Start()
    {
        currentTime = limitTime;
        UpdateTimerText();
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void Update()
    {
        if (isRunning)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                isRunning = false;
                TimeUp();
            }
            UpdateTimerText();
        }
    }

    void TimeUp()
    {
        Debug.Log("時間切れ");
        // ゲームオーバー処理などを書く
    }
}
