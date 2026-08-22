using UnityEngine;
<<<<<<< HEAD
using System.Linq;

public class ShapePuzzleManager : MonoBehaviour
{
    [SerializeField] private ShapePuzzleToolsOrchestrator toolsOrchestrator; // 通信通知用

    [Header("作業者用モジュール")]
    [SerializeField] private ShapePuzzleModule workerModule;

    [Header("使用する図形画像4枚")]
    [SerializeField] private Sprite[] shapeSprites;

    [Header("クリア必要回数")]
    [SerializeField] private int requiredClearCount = 3;
    private int currentClearCount = 0;

    private const int PieceCount = 4;

    // オーケストレーターの OnInitialize イベントから呼ばれる初期化処理
    public void OnInitialize(ShapePuzzleModuleSettingData data)
    {
        currentClearCount = 0;

        if (workerModule != null)
        {
            workerModule.shapeSprites = shapeSprites;
            workerModule.SetupPuzzle(data.correctOrder, data.initialOrder);
        }

        Debug.Log($"【SOからデータ受取完了】図形パズル初期化完了");
    }

    /// <summary>
    /// パズルが揃ったときに ShapePuzzleModule 側から呼ばれる関数
    /// </summary>
    public void OnRoundCleared()
    {
        currentClearCount++;
        Debug.Log($"【図形パズル】クリア進捗: {currentClearCount} / {requiredClearCount}");

        if (currentClearCount >= requiredClearCount)
        {
            GameClear();
        }
        else
        {
            GenerateNextRound();
        }
    }

    /// <summary>
    /// 次の問題（ラウンド）を自前でランダム生成して作業者UIを更新する
    /// </summary>
    private void GenerateNextRound()
    {
        int[] newCorrect = Enumerable.Range(0, PieceCount).ToArray();
        Shuffle(newCorrect);

        int[] newInitial = (int[])newCorrect.Clone();
        do
        {
            Shuffle(newInitial);
        } while (Enumerable.SequenceEqual(newCorrect, newInitial));

        // 作業者画面のパズルを再セットアップ
        if (workerModule != null)
        {
            workerModule.SetupPuzzle(newCorrect, newInitial);
        }

        Debug.Log("【図形パズル】次のラウンドのパズルを生成・更新しました。");
    }

    private void GameClear()
    {
        Debug.Log("【図形パズル】3回クリア達成！GAME CLEAR!");

        // モジュール解除成功を通知（BombManagerと統一）
        if (toolsOrchestrator != null)
        {
            toolsOrchestrator.ModuleSuccess();
        }

        gameObject.SetActive(false);
    }

    private void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
=======
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
>>>>>>> fc563ea5e495d36be31e221292bafd615bc57d12
    }
}