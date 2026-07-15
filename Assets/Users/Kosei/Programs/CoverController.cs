using UnityEngine;
using System.Collections;

public class CoverController : MonoBehaviour
{
    [Header("Cover")]
    public RectTransform cover;

    [Header("Animation")]
    public float animationTime = 0.3f;
    public float closeHeight = 180f;

    private Coroutine animationCoroutine;

    void Start()
    {
        OpenInstant();
    }

    /// <summary>
    /// カバーを閉じる
    /// </summary>
    public void Close()
    {
        StartAnimation(closeHeight);
    }

    /// <summary>
    /// カバーを開く
    /// </summary>
    public void Open()
    {
        StartAnimation(0f);
    }

    /// <summary>
    /// 一瞬で開く
    /// </summary>
    public void OpenInstant()
    {
        if (cover == null)
            return;

        cover.sizeDelta =
            new Vector2(
                cover.sizeDelta.x,
                0f);
    }

    /// <summary>
    /// 一瞬で閉じる
    /// </summary>
    public void CloseInstant()
    {
        if (cover == null)
            return;

        cover.sizeDelta =
            new Vector2(
                cover.sizeDelta.x,
                closeHeight);
    }

    private void StartAnimation(float targetHeight)
    {
        if (cover == null)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine =
            StartCoroutine(Animate(targetHeight));
    }

    private IEnumerator Animate(float targetHeight)
    {
        float startHeight = cover.sizeDelta.y;

        float elapsed = 0f;

        while (elapsed < animationTime)
        {
            elapsed += Time.deltaTime;

            float height = Mathf.Lerp(
                startHeight,
                targetHeight,
                elapsed / animationTime);

            cover.sizeDelta =
                new Vector2(
                    cover.sizeDelta.x,
                    height);

            yield return null;
        }

        cover.sizeDelta =
            new Vector2(
                cover.sizeDelta.x,
                targetHeight);

        animationCoroutine = null;
    }
}
