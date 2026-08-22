using UnityEngine;
using UnityEngine.EventSystems;

public class Wire : MonoBehaviour, IPointerClickHandler
{
    [Header("配線の固有ID (インスペクターで設定)")]
    [SerializeField] private int wireId; 

    [Header("配線のパーツ")]
    [SerializeField] private Transform leftWire;
    [SerializeField] private Transform rightWire;

    [Header("演出の設定")]
    [SerializeField] private float slideDistance = 0.4f;

    private BombManager bombManager;
    private bool isCut = false;

    // 元の位置を記憶しておく変数
    private Vector3 initialLeftPos;
    private Vector3 initialRightPos;

    private void Start()
    {
        bombManager = Object.FindAnyObjectByType<BombManager>();

        // 初期位置（繋がっている状態）を保存
        if (leftWire != null) initialLeftPos = leftWire.localPosition;
        if (rightWire != null) initialRightPos = rightWire.localPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isCut) CutWire();
    }

    private void OnMouseDown()
    {
        if (!isCut) CutWire();
    }

    private void CutWire()
    {
        isCut = true;
        Debug.Log($"【配線切断】Wire ID: {wireId}");
        
        // 左右にスライドして切断
        if (leftWire != null) leftWire.localPosition += new Vector3(slideDistance, 0, 0);
        if (rightWire != null) rightWire.localPosition += new Vector3(-slideDistance, 0, 0);

        // ボムマネージャーに送信
        if (bombManager != null)
        {
            bombManager.OnWireCut(wireId);
        }
    }

    // ★ BombManager から呼ばれるリセット処理（繋がっている状態に戻す）
    public void ResetWire()
    {
        if (!isCut) return; // 切れていないワイヤーはそのまま

        isCut = false;

        // 保存しておいた初期位置に戻す
        if (leftWire != null) leftWire.localPosition = initialLeftPos;
        if (rightWire != null) rightWire.localPosition = initialRightPos;

        Debug.Log($"【配線リセット】Wire ID: {wireId} が元の状態に戻りました。");
    }
}