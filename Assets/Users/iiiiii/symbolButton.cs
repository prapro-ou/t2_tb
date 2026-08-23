using UnityEngine;

public class symbolButton : MonoBehaviour
{
    public int symbolNumber;
    public SymbolManager manager;

    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown検出: " + gameObject.name);

        if (manager == null)
        {
            Debug.LogError(gameObject.name + " の manager が Null です");
            return;
        }

        Debug.Log("Symbol" + symbolNumber + " を送信します");
        manager.PressSymbol(symbolNumber);
    }
}