using UnityEngine;

public class BombManager : MonoBehaviour
{
    [SerializeField] private WireToolsOrchestrator toolsOrchestrator; // 通信通知用

    private int[] correctSequence;
    private int currentStep = 0;
    private bool isGameOver = false;

    // オーケストレーターの OnInitialize イベントから呼ばれる初期化処理
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

        // 正解の配線を選んだ場合
        if (wireId == correctSequence[currentStep])
        {
            currentStep++;
            Debug.Log($"Correct! Step: {currentStep}/{correctSequence.Length}");

            if (currentStep >= correctSequence.Length)
            {
                GameClear();
            }
        }
        // 間違えた配線を選んだ場合（やり直し）
        else
        {
            Debug.LogWarning($"Wrong Wire! Resetting step from {currentStep} back to 0.");
            ResetModule();
        }
    }

    // ★ 間違えた時に最初からやり直す処理
    private void ResetModule()
    {
        currentStep = 0; // ステップを最初に戻す

        // シーン内のすべての Wire スクリプトを探して元に戻す
        Wire[] wires = Object.FindObjectsByType<Wire>();
        foreach (Wire wire in wires)
        {
            wire.ResetWire();
        }

        // ※もし間違えた時に失敗カウントや通知を送りたい場合はここで toolsOrchestrator.ModuleFailed() を呼ぶこともできます。
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
}