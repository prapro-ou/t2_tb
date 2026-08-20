using UnityEngine;

public class symbolButton : MonoBehaviour
{
    public int symbolNumber;
    public SymbolManager manager;

    private void OnMouseDown()
    {
        Debug.Log(gameObject.name + " が押されました");
        manager.PressSymbol(symbolNumber);
    }
}