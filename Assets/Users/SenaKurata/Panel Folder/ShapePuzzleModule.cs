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

    private int[] currentGrid;
    private int[] correctGrid;
    private int firstSelectedIndex = -1;
    private bool isLocked = false; // クリア後の連打・操作防止フラグ

    private void Awake()
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
        if (correctOrder == null || initialOrder == null) return;

        correctGrid = (int[])correctOrder.Clone();
        currentGrid = (int[])initialOrder.Clone();
        isLocked = false; // 操作ロック解除

        int count = Mathf.Min(currentGrid.Length, slotImages.Length);

        for (int i = 0; i < count; i++)
        {
            if (slotImages[i] != null && shapeSprites != null && currentGrid[i] < shapeSprites.Length)
            {
                slotImages[i].sprite = shapeSprites[currentGrid[i]];
            }

            if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
            {
                slotButtons[i].interactable = true;
                slotButtons[i].image.color = Color.white;
            }
        }
        firstSelectedIndex = -1;
    }

    private void OnSlotClicked(int index)
    {
        if (isLocked) return; // ロック中なら操作不可

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

            // 入れ替え
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

    private void CheckClear()
    {
        bool isClear = ShapePuzzleUtility.IsSequenceEqual(currentGrid, correctGrid);

        if (isClear)
        {
            isLocked = true; // 揃ったら操作不可にする
            SetButtonsInteractable(false);
            Debug.Log("★自分のパズルが揃いました！相手の完了を待っています...★");

            if (puzzleManager != null)
            {
                puzzleManager.OnLocalPlayerCleared();
            }
        }
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (var btn in slotButtons)
        {
            if (btn != null) btn.interactable = state;
        }
    }
}