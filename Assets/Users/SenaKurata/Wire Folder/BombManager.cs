using UnityEngine;

public class BombManager : MonoBehaviour
{
    private int[] correctSequence;
    private int currentStep = 0;
    private bool isGameOver = false;

    private void Start()
    {
        // GameData から生成済みの正解データを受け取る
        if (WireData.Instance != null && WireData.Instance.correctSequence != null)
        {
            correctSequence = WireData.Instance.correctSequence;
        }
        else
        {
            Debug.LogError("WireDataが見つかりません！");
        }
    }

    public void OnWireCut(int wireId)
    {
        if (isGameOver || correctSequence == null) return;

        if (wireId == correctSequence[currentStep])
        {
            currentStep++;
            Debug.Log($"Correct! Step: {currentStep}/{correctSequence.Length}");

            if (currentStep >= correctSequence.Length)
            {
                GameClear();
            }
        }
        else
        {
            Explode();
        }
    }

    private void GameClear()
    {
        isGameOver = true;
        Debug.Log("GAME CLEAR!");
    }

    private void Explode()
    {
        isGameOver = true;
        Debug.LogError("BOOM! GAME OVER");
    }
}