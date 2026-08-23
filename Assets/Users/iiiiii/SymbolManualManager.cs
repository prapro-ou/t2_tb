using UnityEngine;

public class SymbolManualManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] manualSprites;
    [SerializeField] private Sprite[] symbolCandidates;

    public void Initialize(SymbolModuleSettingData settingData)
    {
        for (int i = 0; i < 4; i++)
        {
            int buttonNumber = settingData.CorrectOrder[i];
            int symbolIndex = settingData.SymbolIndices[buttonNumber - 1];

            if (symbolIndex < 0 || symbolIndex >= symbolCandidates.Length)
            {
                Debug.LogError("Manual symbol index out of range: " + symbolIndex);
                continue;
            }

            manualSprites[i].sprite = symbolCandidates[symbolIndex];
            manualSprites[i].enabled = true;
            manualSprites[i].sortingOrder = 10;
        }

        Debug.Log(
            "Manual順: "
            + settingData.CorrectOrder[0] + " → "
            + settingData.CorrectOrder[1] + " → "
            + settingData.CorrectOrder[2] + " → "
            + settingData.CorrectOrder[3]
        );
    }
}
