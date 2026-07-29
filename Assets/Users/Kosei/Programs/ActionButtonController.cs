using UnityEngine;
using UnityEngine.UI;

public class ActionButtonController : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public Image iconImage;
    public RectTransform iconRect;

    [Header("Sprites")]
    public Sprite playSprite;
    public Sprite stopSprite;

    [Header("Icon Position")]
    public Vector2 playPosition = Vector2.zero;
    public Vector2 stopPosition = new Vector2(-6f, 0f);

    [Header("Button Colors")]
    public Color normalColor = Color.white;
    public Color successColor = Color.green;
    public Color failedColor = Color.red;
    public Color clearColor = Color.yellow;

    /// <summary>
    /// 再生アイコン表示
    /// </summary>
    public void SetPlay()
    {
        if (iconImage != null)
            iconImage.sprite = playSprite;

        if (iconRect != null)
            iconRect.anchoredPosition = playPosition;

        SetButtonColor(normalColor);
    }

    /// <summary>
    /// 停止アイコン表示
    /// </summary>
    public void SetStop()
    {
        if (iconImage != null)
            iconImage.sprite = stopSprite;

        if (iconRect != null)
            iconRect.anchoredPosition = stopPosition;

        SetButtonColor(normalColor);
    }

    /// <summary>
    /// 成功
    /// </summary>
    public void SetSuccess()
    {
        SetButtonColor(successColor);
    }

    /// <summary>
    /// 失敗
    /// </summary>
    public void SetFailed()
    {
        SetButtonColor(failedColor);
    }

    /// <summary>
    /// モジュールクリア
    /// </summary>
    public void SetClear()
    {
        SetButtonColor(clearColor);
    }

    /// <summary>
    /// ボタン色変更
    /// </summary>
    private void SetButtonColor(Color color)
    {
        if (button == null)
            return;

        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.highlightedColor = color;
        cb.pressedColor = color * 0.9f;
        cb.selectedColor = color;
        cb.disabledColor = color * 0.5f;
        button.colors = cb;
    }
}