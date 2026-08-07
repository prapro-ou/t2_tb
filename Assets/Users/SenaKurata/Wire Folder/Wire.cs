using UnityEngine;

public class Wire : MonoBehaviour
{
    [Header("配線の固有ID (インスペクターで設定)")]
    [SerializeField] private int wireId; 

    [Header("配線のパーツ")]
    [SerializeField] private Transform leftWire;
    [SerializeField] private Transform rightWire;

    [Header("演出の設定")]
    [SerializeField] private float slideDistance = 0.4f; // ★どれくらい間を空けるか

    private BombManager bombManager; // マネージャーへの参照
    private bool isCut = false;

    private void Start()
    {
        // 画面内から BombManager スクリプトを探して自動で紐付ける
        bombManager = Object.FindAnyObjectByType<BombManager>();
    }

    private void OnMouseDown()
    {
        if (!isCut)
        {
            CutWire();
        }
    }

    private void CutWire()
    {
        isCut = true;
        
        // ★回転はさせず、クリックした瞬間にパッと左右にスライドして間を空ける
        leftWire.localPosition += new Vector3(slideDistance, 0, 0);
        rightWire.localPosition += new Vector3(-slideDistance, 0, 0);

        // ボムマネージャーに自分のIDを伝える
        if (bombManager != null)
        {
            bombManager.OnWireCut(wireId);
        }
    }
}