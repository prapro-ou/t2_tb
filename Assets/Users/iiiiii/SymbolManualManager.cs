using UnityEngine;

public class SymbolManualManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] manualSprites;
    [SerializeField] private Sprite[] symbolCandidates;

    public void Initialize(SymbolModuleSettingData settingData)
    {
        Debug.Log("=== SymbolManual Initialize ===");

        Debug.Log(
            "SymbolIndices: " +
            string.Join(", ", settingData.SymbolIndices)
        );

        Debug.Log(
            "CorrectOrder: " +
            string.Join(", ", settingData.CorrectOrder)
        );

        for (int i = 0; i < 4; i++)
        {
            int buttonNumber = settingData.CorrectOrder[i];

            Debug.Log(
                "順番 " + (i + 1) +
                " : buttonNumber = " + buttonNumber
            );

            int symbolIndex =
                settingData.SymbolIndices[buttonNumber - 1];

            Debug.Log(
                "ManualSymbol" + (i + 1) +
                " → symbolCandidates[" + symbolIndex + "]"
            );

            manualSprites[i].sprite =
                symbolCandidates[symbolIndex];

            manualSprites[i].enabled = true;
            manualSprites[i].sortingOrder = 10;
        }
    }
}
