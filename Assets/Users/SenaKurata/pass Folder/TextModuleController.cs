public class TextModuleController : MonoBehaviour
{
    private string correctText; // 受け取った正解文字列
    private string playerInput = ""; // プレイヤーが入力した文字列

    // 初期化時に SO からデータを受け取る関数（プロジェクトの仕様に合わせて調整してください）
    public void Setup(TextModuleSettingData data)
    {
        correctText = data.targetText;
        // UI等のテキスト表示部に正解（またはお題）を表示する
        displayTextUI.text = correctText; 
    }

    // プレイヤーがボタンを押したり入力したときに呼ぶ関数
    public void OnInputCharacter(char inputChar)
    {
        playerInput += inputChar;

        // 文字数が一致したタイミングで判定
        if (playerInput.Length >= correctText.Length)
        {
            CheckAnswer();
        }
    }

    private void CheckAnswer()
    {
        if (playerInput == correctText)
        {
            Debug.Log("⭕ 正解！モジュール解除！");
            // 解除成功処理
        }
        else
        {
            Debug.Log("❌ 不正解！リセットします。");
            playerInput = ""; // 入力をやり直させる場合
            // お手つき・失敗処理
        }
    }
}