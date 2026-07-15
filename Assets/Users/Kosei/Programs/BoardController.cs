using UnityEngine;
using TMPro;

public class BoardController : MonoBehaviour
{
    [Header("Board")]
    public SpriteRenderer[] boardRenderers;

    [Header("UI Text (Optional)")]
    public TMP_Text boardText; // 💡 文字の可読性を上げるためにアタッチ

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color successColor = Color.green;
    public Color failedColor = Color.red;
    public Color clearColor = Color.yellow;

    public void SetNormal()
    {
        SetColor(normalColor);
        if (boardText != null) boardText.color = Color.red; // 通常時の文字色
    }

    public void SetSuccess()
    {
        SetColor(successColor);
        if (boardText != null) boardText.color = Color.black; // 緑背景時は黒文字
    }

    public void SetFailed()
    {
        SetColor(failedColor);
        if (boardText != null) boardText.color = Color.white; // 赤背景時は白文字
    }

    public void SetClear()
    {
        SetColor(clearColor);
        if (boardText != null) boardText.color = Color.black; // 黄背景時は黒文字
    }

    private void SetColor(Color color)
    {
        if (boardRenderers == null) return;

        foreach (SpriteRenderer renderer in boardRenderers)
        {
            if (renderer != null)
                renderer.color = color;
        }
    }
}