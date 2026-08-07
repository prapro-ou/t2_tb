using UnityEngine;

public class BoardController : MonoBehaviour
{
    [Header("Board")]
    public SpriteRenderer[] boardRenderers;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color successColor = Color.green;
    public Color failedColor = Color.red;
    public Color clearColor = Color.yellow;

    public void SetNormal()
    {
        SetColor(normalColor);
    }

    public void SetSuccess()
    {
        SetColor(successColor);
    }

    public void SetFailed()
    {
        SetColor(failedColor);
    }

    public void SetClear()
    {
        SetColor(clearColor);
    }

    private void SetColor(Color color)
    {
        if (boardRenderers == null)
            return;

        foreach (SpriteRenderer renderer in boardRenderers)
        {
            if (renderer != null)
                renderer.color = color;
        }
    }
}