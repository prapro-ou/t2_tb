using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class PasswordButtonModule : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private int _count = -1;
    [SerializeField] private UnityEvent _event;
    [SerializeField] private UnityEvent<int> _eventInt;
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("click: " + _count);
        if (_count < 0)
        {
            _event?.Invoke();
        }
        else
        {
            _eventInt?.Invoke(_count);
        }
    }
}
