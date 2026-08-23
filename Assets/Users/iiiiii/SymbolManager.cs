using UnityEngine;
using System.Collections.Generic;

public class SymbolManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] symbolSprites;
    [SerializeField] private Sprite[] symbolCandidates;

    private int[] correctOrder;
    private int currentIndex = 0;

    public void Initialize(SymbolModuleSettingData settingData)
    {
        currentIndex = 0;

        SetSymbols(settingData.SymbolIndices);
        correctOrder = settingData.CorrectOrder.ToArray();

        Debug.Log("今回の正解順: " + string.Join(" → ", correctOrder));
    }

    private void SetSymbols(List<int> symbolIndices)
    {
        for (int i = 0; i < 4; i++)
        {
            int index = symbolIndices[i];

            if (index < 0 || index >= symbolCandidates.Length)
            {
                Debug.LogError("Symbol index out of range: " + index);
                continue;
            }

            symbolSprites[i].enabled = true;
            symbolSprites[i].sprite = symbolCandidates[index];

            // 背景より前に表示
            symbolSprites[i].sortingOrder = 10;
        }
    }

    public void PressSymbol(int symbolNumber)
    {
        if (correctOrder == null || correctOrder.Length == 0)
        {
            Debug.LogWarning("correctOrder がまだ設定されていません。");
            return;
        }

        if (symbolNumber == correctOrder[currentIndex])
        {
            Debug.Log("正解: Symbol" + symbolNumber);

            currentIndex++;

            if (currentIndex >= correctOrder.Length)
            {
                Debug.Log("解除成功！");
                currentIndex = 0;
            }
        }
        else
        {
            Debug.Log("ミス！");
            currentIndex = 0;
        }
    }
}