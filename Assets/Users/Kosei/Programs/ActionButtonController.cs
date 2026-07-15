using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems; // 👈 必須

public class ActionButtonController : MonoBehaviour, IPointerDownHandler // 👈 追加
{
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer buttonRenderer;
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Sprites")]
    public Sprite playSprite;
    public Sprite stopSprite;

    [Header("Icon Position")]
    public Vector3 playPosition = Vector3.zero;
    public Vector3 stopPosition = new Vector3(-0.06f, 0f, 0f);

    [Header("Button Colors")]
    public Color normalColor = Color.white;
    public Color successColor = Color.green;
    public Color failedColor = Color.red;
    public Color clearColor = Color.yellow;

    [Header("Click Event")]
    public UnityEvent onClick;

    private Color currentBaseColor;

    private void Awake()
    {
        currentBaseColor = normalColor;
    }

    // 💡 DialModule と同じ EventSystem 経由のクリック検知
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("【ActionButtonController】PointerDown 検出！");
        onClick?.Invoke();
    }

    public void SetPlay()
    {
        if (iconRenderer != null) iconRenderer.sprite = playSprite;
        if (iconRenderer != null) iconRenderer.transform.localPosition = playPosition;
        SetButtonColor(normalColor);
    }

    public void SetStop()
    {
        if (iconRenderer != null) iconRenderer.sprite = stopSprite;
        if (iconRenderer != null) iconRenderer.transform.localPosition = stopPosition;
        SetButtonColor(normalColor);
    }

    public void SetSuccess() => SetButtonColor(successColor);
    public void SetFailed() => SetButtonColor(failedColor);
    public void SetClear() => SetButtonColor(clearColor);

    private void SetButtonColor(Color color)
    {
        currentBaseColor = color;
        if (buttonRenderer != null) buttonRenderer.color = color;
        if (iconRenderer != null) iconRenderer.color = color;
    }
}