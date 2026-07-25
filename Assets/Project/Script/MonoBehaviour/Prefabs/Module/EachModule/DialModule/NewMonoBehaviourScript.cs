using UnityEngine;
using UnityEngine.InputSystem;

public class DialManagerInputSystem : MonoBehaviour
{
    [SerializeField] private Transform _dialTarget;    // 回転させたいダイヤルのTransform
    [SerializeField] private Collider2D _dialCollider; // クリック判定用Collider
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData;

}