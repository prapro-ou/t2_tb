using UnityEngine;
using UnityEngine.UI;

public class ShapePuzzleModule : MonoBehaviour
{
    [Header("UI設定")]
    public Button[] slotButtons = new Button[4];
    public Image[] slotImages = new Image[4];
    public Sprite[] shapeSprites;

    [Header("親マネージャーへの参照")]
    [SerializeField] private ShapePuzzleManager puzzleManager;

    private int[] currentGrid = new int[4];
    private int[] correctGrid = new int[4];
    private int firstSelectedIndex = -1;

    private void Start()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
            {
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
            }
        }
    }

    public void SetupPuzzle(int[] correctOrder, int[] initialOrder)
    {
        correctGrid = (int[])correctOrder.Clone();
        currentGrid = (int[])initialOrder.Clone();

        for (int i = 0; i < 4; i++)
        {
            if (slotImages != null && i < slotImages.Length && slotImages[i] != null &&
                shapeSprites != null && currentGrid[i] < shapeSprites.Length)
            {
                slotImages[i].sprite = shapeSprites[currentGrid[i]];
            }

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
            firstSelectedIndex = index;
            if (slotButtons[index] != null)
            {
                slotButtons[index].image.color = Color.yellow;
            }
        }
        else
        {
            if (firstSelectedIndex == index)
            {
                if (slotButtons[firstSelectedIndex] != null)
                {
                    slotButtons[firstSelectedIndex].image.color = Color.white;
                }
                firstSelectedIndex = -1;
                return;
            }

            // 画像とデータの入れ替え
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
            Debug.Log("★このラウンドのパズルが揃いました！★");
            if (puzzleManager != null)
            {
                puzzleManager.OnRoundCleared();
            }
        }
    }
}