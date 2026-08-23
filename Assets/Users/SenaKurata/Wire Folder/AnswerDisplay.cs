using UnityEngine;
using TMPro;

public class AnswerDisplay : MonoBehaviour
{
    [System.Serializable]
    public struct WireColorData
    {
        public string colorName;
        public Color color;
    }

    [Header("UI設定")]
    [SerializeField] private TextMeshProUGUI guideText;

    [Header("配線の設定 (ID順に設定)")]
    [SerializeField] private WireColorData[] wireColorDefinitions = new WireColorData[]
    {
        new WireColorData { colorName = "RED", color = Color.red },
        new WireColorData { colorName = "BLUE", color = Color.blue },
        new WireColorData { colorName = "YELLOW", color = Color.yellow },
        new WireColorData { colorName = "GREEN", color = Color.green }
    };

    // オーケストレーターの OnInitialize イベントから呼ばれるメソッド
    public void OnInitialize(WireModuleSettingData data)
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

    // （以降の処理はそのまま）

        string sequenceText = "Code: ";

        for (int i = 0; i < sequence.Length; i++)
        {
            int wireId = sequence[i];

            WireColorData info = GetWireColorInfo(wireId);
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

    private WireColorData GetWireColorInfo(int wireId)
    {
        if (wireColorDefinitions != null && wireId >= 0 && wireId < wireColorDefinitions.Length)
        {
            return wireColorDefinitions[wireId];
        }

        // 定義外のIDが渡された場合のフォールバック
        return new WireColorData 
        { 
            colorName = $"WIRE{wireId}", 
            color = Color.white 
        };
    }
}