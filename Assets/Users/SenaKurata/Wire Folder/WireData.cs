using UnityEngine;
using System.Collections.Generic;

public class WireData : MonoBehaviour
{
    public static WireData Instance { get; private set; }

    [Header("配線の総数")]
    [SerializeField] private int totalWires = 4;

    // 生成された正解データ（両方の画面からこれを参照する）
    public int[] correctSequence;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // ★ゲーム開始（オブジェクト生成時）に1回だけランダム生成！
        GenerateSequence();
    }

    // 正解の切断順番をランダム生成する処理
    public void GenerateSequence()
    {
        correctSequence = new int[totalWires];

        List<int> numbers = new List<int>();
        for (int i = 0; i < totalWires; i++)
        {
            numbers.Add(i);
        }

        // Fisher-Yates シャッフル
        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = numbers[i];
            numbers[i] = numbers[randomIndex];
            numbers[randomIndex] = temp;
        }

        for (int i = 0; i < totalWires; i++)
        {
            correctSequence[i] = numbers[i];
        }

        Debug.Log($"【ゲーム開始】今回の正解コード: {string.Join(", ", correctSequence)}");
    }
}