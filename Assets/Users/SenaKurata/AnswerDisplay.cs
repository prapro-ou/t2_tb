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

    private void Start()
    {
        DisplayAnswer();
    }

    public void DisplayAnswer()
    {
        if (guideText == null) return;

        // GameDataから保存された正解データを取得
        if (WireData.Instance == null || WireData.Instance.correctSequence == null)
        {
            guideText.text = "Code: No Data";
            return;
        }

        int[] sequence = WireData.Instance.correctSequence;
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
    }

    private Color GetWireColor(int wireId)
    {
        if (wireColors != null && wireId < wireColors.Length)
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