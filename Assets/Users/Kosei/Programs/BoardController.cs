using UnityEngine;
using UnityEngine.UI;

public class BoardController : MonoBehaviour
{
    [Header("Board")]
    public Image boardImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color successColor = Color.green;
    public Color failedColor = Color.red;
    public Color clearColor = Color.yellow;

    public void SetNormal()
    {
        if (boardImage != null)
            boardImage.color = normalColor;
    }

    public void SetSuccess()
    {
        if (boardImage != null)
            boardImage.color = successColor;
    }

    public void SetFailed()
    {
        if (boardImage != null)
            boardImage.color = failedColor;
    }

    public void SetClear()
    {
        if (boardImage != null)
            boardImage.color = clearColor;
    }
}