using UnityEngine;

public class BombManager : MonoBehaviour
{
    [Header("配線の総数 (インスペクターで設定)")]
    [SerializeField] private int totalWires = 4;

    [Header("正解の切断順番 (自動生成されるため非公開へ、確認用)")]
    [SerializeField] private int[] correctSequence; 

    private int currentStep = 0; 
    private bool isGameOver = false;

    private void Start()
    {
        // ★ゲーム開始時に順番を決める関数を呼び出す
        InitializeSequence();
    }

    // ★指示された「順番を指定する（生成する）」関数
    private void InitializeSequence()
    {
        // 配列の領域を確保
        correctSequence = new int[totalWires];

        // --- ここを後からランダム処理に書き換える ---
        // 今はとりあえず動作確認用に固定の順番 (2, 0, 3, 1) を入れておきます
        correctSequence[0] = 2;
        correctSequence[1] = 0;
        correctSequence[2] = 3;
        correctSequence[3] = 1;
        // ------------------------------------------

        Debug.Log("【初期化】切断の順番が決定しました。");
    }

    public void OnWireCut(int wireId)
    {
        if (isGameOver) return;

        if (wireId == correctSequence[currentStep])
        {
            currentStep++;
            Debug.Log($"正解！次の配線へ。進捗: {currentStep}/{correctSequence.Length}");

            if (currentStep >= correctSequence.Length)
            {
                GameClear();
            }
        }
        else
        {
            Explode();
        }
    }

    private void GameClear()
    {
        isGameOver = true;
        Debug.Log("【ゲームクリア】爆弾の解除に成功しました！");
    }

    private void Explode()
    {
        isGameOver = true;
        Debug.LogError("【ゲームオーバー】ドカーン！間違った配線を切ったため爆発しました！");
    }
}