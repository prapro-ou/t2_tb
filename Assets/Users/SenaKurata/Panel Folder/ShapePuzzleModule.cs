using UnityEngine;
using UnityEngine.UI;

public class ShapePuzzleModule : MonoBehaviour
{
    public Button[] slotButtons = new Button[4];
    public Image[] slotImages = new Image[4];
    public Sprite[] shapeSprites;

    private int[] currentGrid = new int[4];
    private int[] correctGrid = new int[4];
    private int firstSelectedIndex = -1;

    private void Start()
    {
        // ボタンのクリックイベントを安全に登録
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            // ★安全装置：ボタンが存在する場合のみ登録する
            if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
            {
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
            }
        }
    }

    // GameManagerから正解と初期配置を受け取る関数
    public void SetupPuzzle(int[] correctOrder, int[] initialOrder)
    {
        correctGrid = (int[])correctOrder.Clone();
        currentGrid = (int[])initialOrder.Clone();

        for (int i = 0; i < 4; i++)
        {
            // ★安全装置：画像が正しく設定されている場合のみ反映
            if (slotImages != null && i < slotImages.Length && slotImages[i] != null &&
                shapeSprites != null && currentGrid[i] < shapeSprites.Length)
            {
                slotImages[i].sprite = shapeSprites[currentGrid[i]];
            }

            // ★安全装置：ボタンの色を初期化
            if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
            {
                slotButtons[i].image.color = Color.white;
            }
        }
        firstSelectedIndex = -1;
    }

    void OnSlotClicked(int index)
    {
        if (firstSelectedIndex == -1)
        {
            // 1回目の選択（黄色くする）
            firstSelectedIndex = index;
            if (slotButtons[index] != null)
            {
                slotButtons[index].image.color = Color.yellow;
            }
        }
        else
        {
            // 同じボタンを押したらキャンセル
            if (firstSelectedIndex == index)
            {
                if (slotButtons[firstSelectedIndex] != null)
                {
                    slotButtons[firstSelectedIndex].image.color = Color.white;
                }
                firstSelectedIndex = -1;
                return;
            }

            // 画像と数値データの入れ替え
            int temp = currentGrid[firstSelectedIndex];
            currentGrid[firstSelectedIndex] = currentGrid[index];
            currentGrid[index] = temp;

            if (slotImages[firstSelectedIndex] != null)
                slotImages[firstSelectedIndex].sprite = shapeSprites[currentGrid[firstSelectedIndex]];
            
            if (slotImages[index] != null)
                slotImages[index].sprite = shapeSprites[currentGrid[index]];

            if (slotButtons[firstSelectedIndex] != null)
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
            Debug.Log("★パズルクリア！正解です！★");
        }
    }
}