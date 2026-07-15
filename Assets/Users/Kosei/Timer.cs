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

            timerText.text = Mathf.Ceil(currentTime).ToString();
        }
    }

    void TimeUp()
    {
        Debug.Log("時間切れ");
        // ゲームオーバー処理などを書く
    }
}
