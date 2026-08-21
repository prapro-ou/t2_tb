using UnityEngine;
using TMPro;

public class AnswerDisplay : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private TextMeshProUGUI guideText;

    [Header("配線の色設定 (BombManagerと要素順を揃える)")]
    [SerializeField] private Color[] wireColors = new Color[] 
    { 
        Color.red, 
        Color.blue, 
        Color.yellow, 
        Color.green 
    };

    // 💡 オーケストレーターの OnInitialize イベントから呼ばれるメソッド
    public void OnInitialize(WireModuleSettingData data)
    {
        if (data.correctSequence == null) return;

        DisplayAnswer(data.correctSequence);
    }

    public void DisplayAnswer(int[] sequence)
    {
        if (guideText == null) return;

        string sequenceText = "Code: ";

        for (int i = 0; i < sequence.Length; i++)
        {
            int wireId = sequence[i];

            string colorName = GetColorName(wireId);
            string hexColor = ColorUtility.ToHtmlStringRGB(GetWireColor(wireId));

            // Rich Text形式でカラー表示
            sequenceText += $"<color=#{hexColor}>{colorName}</color>";

            if (i < sequence.Length - 1)
            {
                sequenceText += " -> ";
            }
        }

        guideText.text = sequenceText;
        Debug.Log($"【解答表示完了】{sequenceText}");
    }

    private Color GetWireColor(int wireId)
    {
        if (wireColors != null && wireId >= 0 && wireId < wireColors.Length)
        {
            return wireColors[wireId];
        }
        return Color.white;
    }

    private string GetColorName(int wireId)
    {
        Color color = GetWireColor(wireId);

        if (color == Color.red) return "RED";
        if (color == Color.blue) return "BLUE";
        if (color == Color.yellow) return "YELLOW";
        if (color == Color.green) return "GREEN";
        if (color == Color.white) return "WHITE";
        if (color == Color.black) return "BLACK";

        return $"WIRE{wireId}";
    }
}