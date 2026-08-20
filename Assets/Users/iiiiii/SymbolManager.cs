using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class SymbolData
{
    public string text;
    public Sprite sprite;
    public bool useSprite;
}

public class SymbolManager : MonoBehaviour
{
    [SerializeField] private TMP_Text[] symbolTexts;
    [SerializeField] private SpriteRenderer[] symbolSprites;

    [SerializeField] private SymbolData[] symbolCandidates;

    private int[] correctOrder = { 1, 2, 3, 4 };
    private int currentIndex = 0;

    void Start()
    {
        SetRandomSymbols();
        ShuffleOrder();

        Debug.Log(
            "今回の正解順: "
            + correctOrder[0] + " → "
            + correctOrder[1] + " → "
            + correctOrder[2] + " → "
            + correctOrder[3]
        );
    }

    void SetRandomSymbols()
    {
        List<SymbolData> candidates = new List<SymbolData>(symbolCandidates);

        for (int i = 0; i < 4; i++)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            SymbolData selected = candidates[randomIndex];

            if (selected.useSprite)
            {
                symbolTexts[i].gameObject.SetActive(false);
                symbolSprites[i].gameObject.SetActive(true);
                symbolSprites[i].sprite = selected.sprite;
            }
            else
            {
                symbolSprites[i].gameObject.SetActive(false);
                symbolTexts[i].gameObject.SetActive(true);
                symbolTexts[i].text = selected.text;
            }

            candidates.RemoveAt(randomIndex);
        }
    }

    void ShuffleOrder()
    {
        for (int i = correctOrder.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = correctOrder[i];
            correctOrder[i] = correctOrder[randomIndex];
            correctOrder[randomIndex] = temp;
        }
    }

    public void PressSymbol(int symbolNumber)
    {
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