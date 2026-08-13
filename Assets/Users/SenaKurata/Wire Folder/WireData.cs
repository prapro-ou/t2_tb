using UnityEngine;

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

        // 💡【変更】Awake での GenerateSequence(); は削除しました！
        // データは SO 経由で BombManager からセットされます。
    }
}