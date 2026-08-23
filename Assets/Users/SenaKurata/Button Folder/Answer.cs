using UnityEngine;
using TMPro;

public class Answer : MonoBehaviour
{
    [System.Serializable]
    public struct ButtonColorData
    {
        public string colorName;
        public Color color;
    }

    [Header("UI設定")]
    [SerializeField] private TextMeshProUGUI guideText;

    [Header("ボタンの設定 (ID順に設定)")]
    [SerializeField] private ButtonColorData[] buttonColorDefinitions = new ButtonColorData[]
    {
        new ButtonColorData { colorName = "RED", color = Color.red },
        new ButtonColorData { colorName = "BLUE", color = Color.blue },
        new ButtonColorData { colorName = "YELLOW", color = Color.yellow },
        new ButtonColorData { colorName = "GREEN", color = Color.green }
    };

    // オーケストレーターの OnInitialize イベントから呼ばれるメソッド
    // ※引数の型 (ButtonModuleSettingData) はお使いのクラス名に合わせて変更してください
    public void OnInitialize(ButtonModuleSettingData data)
    {
        Debug.Log($"【AnswerDisplay】OnInitializeが呼び出されました！データ存在: {data.correctSequence != null}");

        if (data.correctSequence == null)
        {
            Debug.LogError("【AnswerDisplay】correctSequence が null です！データが正しく渡されていません。");
            return;
        }

        DisplayAnswer(data.correctSequence);
    }

    public void DisplayAnswer(int[] sequence)
    {
        if (guideText == null)
        {
            Debug.LogError("【AnswerDisplay】guideText (TextMeshProUGUI) が Inspector で未設定です！");
            return;
        }

        string sequenceText = "Code: ";

        for (int i = 0; i < sequence.Length; i++)
        {
            int buttonId = sequence[i];

            ButtonColorData info = GetButtonColorInfo(buttonId);
            string hexColor = ColorUtility.ToHtmlStringRGB(info.color);

            // Rich Text形式でカラー表示
            sequenceText += $"<color=#{hexColor}>{info.colorName}</color>";

            if (i < sequence.Length - 1)
            {
                sequenceText += " -> ";
            }
        }

        guideText.text = sequenceText;
        Debug.Log($"【解答表示完了】{sequenceText}");
    }

    private ButtonColorData GetButtonColorInfo(int buttonId)
    {
        if (buttonColorDefinitions != null && buttonId >= 0 && buttonId < buttonColorDefinitions.Length)
        {
            return buttonColorDefinitions[buttonId];
        }

        // 定義外のIDが渡された場合のフォールバック
        return new ButtonColorData 
        { 
            colorName = $"BUTTON{buttonId}", 
            color = Color.white 
        };
    }
}