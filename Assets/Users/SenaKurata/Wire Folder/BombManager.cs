using UnityEngine;

public class BombManager : MonoBehaviour
{
    [SerializeField] private WireToolsOrchestrator toolsOrchestrator; // 💡 通信通知用

    private int[] correctSequence;
    private int currentStep = 0;
    private bool isGameOver = false;

    // 💡 オーケストレーターの OnInitialize イベントから呼ばれる初期化処理
    public void OnInitialize(WireModuleSettingData data)
    {
        this.correctSequence = data.correctSequence;
        this.currentStep = 0;
        this.isGameOver = false;

        Debug.Log($"【SOからデータ受取完了】正解コード: {string.Join(", ", correctSequence)}");
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
        
        // モジュール解除成功を通知
        if (toolsOrchestrator != null)
        {
            toolsOrchestrator.ModuleSuccess();
        }
    }

    private void Explode()
    {
        isGameOver = true;
        Debug.LogError("BOOM! GAME OVER");

        // モジュール解除失敗を通知
        if (toolsOrchestrator != null)
        {
            toolsOrchestrator.ModuleFailed();
        }
    }
}