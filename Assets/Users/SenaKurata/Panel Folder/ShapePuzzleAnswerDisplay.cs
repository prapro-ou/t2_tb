using UnityEngine;
using UnityEngine.UI;

public class ShapePuzzleAnswerDisplay : MonoBehaviour
{
    [Header("指示役用UI（正解表示用Image 4つ）")]
    [SerializeField] private Image[] manualAnswerImages = new Image[4];

    [Header("使用する図形画像4枚")]
    [SerializeField] private Sprite[] shapeSprites;

    public void OnInitialize(ShapePuzzleModuleSettingData data)
    {
        if (data.correctOrder == null || data.correctOrder.Length == 0) return;

        for (int i = 0; i < manualAnswerImages.Length && i < data.correctOrder.Length; i++)
        {
            int spriteIndex = data.correctOrder[i];
            if (spriteIndex >= 0 && spriteIndex < shapeSprites.Length)
            {
                manualAnswerImages[i].sprite = shapeSprites[spriteIndex];
            }
        }
    }
}