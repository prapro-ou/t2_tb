using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class SpriteButton2D : MonoBehaviour
{
    [SerializeField] private UnityEvent onClick;

    // 2Dコライダー上でマウスが押されたときに自動発火
    private void OnMouseDown()
    {
        Debug.Log("【SpriteButton2D】ボタンが押されました");
        onClick?.Invoke();
    }
}