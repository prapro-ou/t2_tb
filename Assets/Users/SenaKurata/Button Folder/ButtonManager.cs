using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private ButtonToolsOrchestrator toolsOrchestrator; // 通信通知用 (オーケストレーターの型名に合わせて適宜調整してください)

    private int[] correctSequence;
    private int currentStep = 0;
    private bool isGameOver = false;

    // オーケストレーターの OnInitialize イベントから呼ばれる初期化処理
    public void OnInitialize(ButtonModuleSettingData data)
    {
        this.correctSequence = data.correctSequence;
        this.currentStep = 0;
        this.isGameOver = false;

        Debug.Log($"【SOからデータ受取完了】正解コード: {string.Join(", ", correctSequence)}");
    }

    // ボタンが押された時に Button スクリプトから呼ばれる処理
    public void OnButtonClicked(int buttonId)
    {
        if (isGameOver || correctSequence == null) return;

        // 正解のボタンを選んだ場合
        if (buttonId == correctSequence[currentStep])
        {
            currentStep++;
            Debug.Log($"Correct! Step: {currentStep}/{correctSequence.Length}");

            if (currentStep >= correctSequence.Length)
            {
                GameClear();
            }
        }
        // 間違えたボタンを選んだ場合（やり直し）
        else
        {
            Debug.LogWarning($"Wrong Button! Resetting step from {currentStep} back to 0.");
            ResetModule();
        }
    }

    // ★ 間違えた時に最初からやり直す処理
    private void ResetModule()
    {
        currentStep = 0; // ステップを最初に戻す

        // シーン内のすべての Button スクリプトを探して元の状態に戻す
        Buttonset[] buttons = Object.FindObjectsByType<Buttonset>();
        foreach (Buttonset button in buttons)
        {
            button.ResetButton();
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