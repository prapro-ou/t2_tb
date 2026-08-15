using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShapePuzzleModule : MonoBehaviour
{
    public Button[] slotButtons = new Button[4];
    public Image[] slotImages = new Image[4];
    public Sprite[] shapeSprites;

    private int[] currentGrid = new int[4];
    private int[] correctGrid = new int[4];
    private int firstSelectedIndex = -1;

    // GameManagerから正解と初期配置を受け取る関数
    public void SetupPuzzle(int[] correctOrder, int[] initialOrder)
    {
        correctGrid = (int[])correctOrder.Clone();
        currentGrid = (int[])initialOrder.Clone();

        for (int i = 0; i < 4; i++)
        {
            slotImages[i].sprite = shapeSprites[currentGrid[i]];
            slotButtons[i].image.color = Color.white;
        }
        firstSelectedIndex = -1;
    }

    private void Start()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
        }
    }

    void OnSlotClicked(int index)
    {
        if (firstSelectedIndex == -1)
        {
            firstSelectedIndex = index;
            slotButtons[index].image.color = Color.yellow;
        }
        else
        {
            if (firstSelectedIndex == index)
            {
                slotButtons[firstSelectedIndex].image.color = Color.white;
                firstSelectedIndex = -1;
                return;
            }

            // 画像と数値の入れ替え
            int temp = currentGrid[firstSelectedIndex];
            currentGrid[firstSelectedIndex] = currentGrid[index];
            currentGrid[index] = temp;

            slotImages[firstSelectedIndex].sprite = shapeSprites[currentGrid[firstSelectedIndex]];
            slotImages[index].sprite = shapeSprites[currentGrid[index]];

            slotButtons[firstSelectedIndex].image.color = Color.white;
            firstSelectedIndex = -1;

            CheckClear();
        }
    }

    void CheckClear()
    {
        bool isClear = true;
        for (int i = 0; i < 4; i++)
        {
            if (currentGrid[i] != correctGrid[i])
            {
                isClear = false;
                break;
            }
        }

        if (isClear)
        {
            Debug.Log("★パズルクリア！★");
        }
    }
}