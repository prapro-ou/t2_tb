using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class Buttonset : MonoBehaviour, IPointerClickHandler
{
    [Header("ボタンの固有ID (インスペクターで設定)")]
    [SerializeField] private int buttonId;

    [Header("表示・演出の設定")]
    [SerializeField] private SpriteRenderer spriteRenderer; // ボタン描画用のSpriteRenderer
    [SerializeField] private Sprite normalSprite;          // 通常時（1枚目・オフ）の画像
    [SerializeField] private Sprite pressedSprite;         // 押した時（2枚目・オン）の画像

    [Header("押し込み・錯覚調整の設定")]
    [SerializeField] private float pressDepth = 0.1f;        // 押した時に沈む深さ(Y軸方向)
    [SerializeField] private float pressedScaleRatio = 0.8f; // 押した時の拡大率(0.9で10%縮小して膨張感を抑える)

    private ButtonManager buttonManager;
    private bool isPressed = false;
    private Vector3 initialPos;
    private Vector3 initialScale;

    private void Start()
    {
        buttonManager = Object.FindAnyObjectByType<ButtonManager>();

        // SpriteRendererの自動取得
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 初期画像のセット
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        // 初期位置と初期スケールの記憶
        initialPos = transform.localPosition;
        initialScale = transform.localScale;
    }

    // UI/2D EventSystem経由でのクリック
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isPressed) PressButton();
    }

    // コライダー経由でのクリック
    private void OnMouseDown()
    {
        if (!isPressed) PressButton();
    }

    private void PressButton()
    {
        isPressed = true;
        Debug.Log($"【ボタン押下】Button ID: {buttonId}");

        // 1. 画像を光っている2枚目に切り替え
        if (spriteRenderer != null && pressedSprite != null)
        {
            spriteRenderer.sprite = pressedSprite;
        }

        // 2. Y軸の押し込み（下に沈める）
        transform.localPosition = initialPos - new Vector3(0, pressDepth, 0);

        // 3. スケールの調整（膨張感を抑えるために指定比率に縮小）
        transform.localScale = initialScale * pressedScaleRatio;

        // 4. マネージャーへ通知
        if (buttonManager != null)
        {
            buttonManager.OnButtonClicked(buttonId);
        }
    }

    // ★ ButtonManager から呼ばれるリセット処理
    public void ResetButton()
    {
        if (!isPressed) return;

        isPressed = false;

        // 1. 画像を元の1枚目に戻す
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        // 2. 位置とスケールを元に戻す
        transform.localPosition = initialPos;
        transform.localScale = initialScale;

        Debug.Log($"【ボタンリセット】Button ID: {buttonId} が元の状態に戻りました。");
    }
}