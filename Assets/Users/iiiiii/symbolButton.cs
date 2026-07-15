using UnityEngine;

public class symbolButton : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log(gameObject.name + " が押されました");
    }
}
