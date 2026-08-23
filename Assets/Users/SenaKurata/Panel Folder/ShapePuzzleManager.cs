using UnityEngine;
using System.Linq;

public class ShapePuzzleManager : MonoBehaviour
{
    [SerializeField] private ShapePuzzleToolsOrchestrator toolsOrchestrator;

    [Header("パズルモジュール（手元画面）")]
    [SerializeField] private ShapePuzzleModule puzzleModule;

    [Header("使用する図形画像4枚")]
    [SerializeField] private Sprite[] shapeSprites;

    [Header("クリア必要回数")]
    [SerializeField] private int requiredClearCount = 3;
    private int currentClearCount = 0;

    // 2人のクリアフラグ
    private bool isLocalCleared = false;
    private bool isRemoteCleared = false;

    private const int PieceCount = 4;

    public void OnInitialize(ShapePuzzleModuleSettingData data)
    {
        currentClearCount = 0;
        ResetRoundState();

        if (puzzleModule != null)
        {
            puzzleModule.shapeSprites = shapeSprites;
            puzzleModule.SetupPuzzle(data.correctOrder, data.initialOrder);
        }

        Debug.Log("【図形パズル】初期化完了");
    }

    /// <summary>
    /// 自分の画面でパズルが揃ったとき（Moduleから呼ばれる）
    /// </summary>
    public void OnLocalPlayerCleared()
    {
        if (isLocalCleared) return;
        isLocalCleared = true;

        // オーケストレーター経由で「自分が揃った」ことを相手端末へ送信する処理が必要な場合はここで行う
        CheckBothPlayersCleared();
    }

    /// <summary>
    /// P2Pで相手プレイヤーが揃った通知をオーケストレーターから受信したとき呼び出す関数
    /// </summary>
    public void OnRemotePlayerCleared()
    {
        if (isRemoteCleared) return;
        isRemoteCleared = true;
        Debug.Log("【図形パズル】相手プレイヤーのパズルが揃いました！");

        CheckBothPlayersCleared();
    }

    /// <summary>
    /// 両プレイヤーが揃ったか判定
    /// </summary>
    private void CheckBothPlayersCleared()
    {
        if (isLocalCleared && isRemoteCleared)
        {
            currentClearCount++;
            Debug.Log($"★2人とも完了！ ラウンド進捗: {currentClearCount} / {requiredClearCount}★");

            if (currentClearCount >= requiredClearCount)
            {
                GameClear();
            }
            else
            {
                GenerateNextRound();
            }
        }
    }

    private void GenerateNextRound()
    {
        ResetRoundState();

        int[] newCorrect = Enumerable.Range(0, PieceCount).ToArray();
        ShapePuzzleUtility.Shuffle(newCorrect);

        int[] newInitial = (int[])newCorrect.Clone();
        int safetyCount = 0;
        do
        {
            ShapePuzzleUtility.Shuffle(newInitial);
            safetyCount++;
        } while (ShapePuzzleUtility.IsSequenceEqual(newCorrect, newInitial) && safetyCount < 100);

        if (puzzleModule != null)
        {
            puzzleModule.SetupPuzzle(newCorrect, newInitial);
        }

        Debug.Log("【図形パズル】次のラウンドを開始します。");
    }

    private void ResetRoundState()
    {
        isLocalCleared = false;
        isRemoteCleared = false;
    }

    private void GameClear()
    {
        Debug.Log("【図形パズル】全ラウンドクリア達成！");

        if (toolsOrchestrator != null)
        {
            toolsOrchestrator.ModuleSuccess();
        }

        gameObject.SetActive(false);
    }
}