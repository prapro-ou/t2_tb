using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShapePuzzleManager : MonoBehaviour
{
    [Header("作業者用モジュール")]
    public ShapePuzzleModule workerModule;

    [Header("指示役用UI（正解表示用Image 4つ）")]
    public Image[] manualAnswerImages = new Image[4];

    [Header("使用する図形画像4枚")]
    public Sprite[] shapeSprites;

    void Start()
    {
        GenerateRandomPuzzle();
    }

    public void GenerateRandomPuzzle()
    {
        // 1. 正解の並び順をランダムに決定 (例: [2, 0, 3, 1])
        List<int> correctList = new List<int> { 0, 1, 2, 3 };
        ShuffleList(correctList);
        int[] correctOrder = correctList.ToArray();

        // 2. 指示役の画面に「正解の画像」をセット
        for (int i = 0; i < 4; i++)
        {
            manualAnswerImages[i].sprite = shapeSprites[correctOrder[i]];
        }

        // 3. 作業者用にシャッフルされた初期配置を作成（正解と被らないように調整）
        List<int> initialList = new List<int>(correctList);
        do
        {
            ShuffleList(initialList);
        } while (IsSameArray(correctOrder, initialList.ToArray()));

        // 4. 作業者画面にデータをセットしてパズル開始
        workerModule.shapeSprites = shapeSprites;
        workerModule.SetupPuzzle(correctOrder, initialList.ToArray());
    }

    // リストをシャッフルする補助関数
    void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // 配列が完全一致しているか調べる補助関数
    bool IsSameArray(int[] a, int[] b)
    {
        for (int i = 0; i < 4; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }
}