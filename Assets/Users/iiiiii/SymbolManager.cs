using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SymbolManager : MonoBehaviour
{
    // 記号画像
    [SerializeField] private SpriteRenderer[] symbolSprites;

    // ボタン本体
    [SerializeField] private SpriteRenderer[] buttonSprites;

    // 記号候補
    [SerializeField] private Sprite[] symbolCandidates;

    private int[] correctOrder;
    private int currentIndex = 0;

    public void Initialize(SymbolModuleSettingData settingData)
    {
        currentIndex = 0;

        SetSymbols(settingData.SymbolIndices);
        correctOrder = settingData.CorrectOrder.ToArray();

        // 開始時は青
        SetButtonColor(Color.blue);

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

            // ボタンより前に表示
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

                // 全て正解したら緑
                SetButtonColor(Color.green);

                currentIndex = 0;
            }
        }
        else
        {
            Debug.Log("ミス！");

            // 最初からやり直し
            currentIndex = 0;

            // 一瞬赤くしてから青へ戻す
            StartCoroutine(FlashRed());
        }
    }

    private IEnumerator FlashRed()
    {
        // 赤にする
        SetButtonColor(Color.red);

        // 0.3秒間赤
        yield return new WaitForSeconds(0.3f);

        // 通常の青に戻す
        SetButtonColor(Color.blue);
    }

    private void SetButtonColor(Color color)
    {
        for (int i = 0; i < buttonSprites.Length; i++)
        {
            buttonSprites[i].color = color;
        }
    }
}