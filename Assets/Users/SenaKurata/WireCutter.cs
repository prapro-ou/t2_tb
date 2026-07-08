using UnityEngine;
// 新しいInput Systemを使うためにこの一行を追加します
using UnityEngine.InputSystem; 

public class WireCutter : MonoBehaviour
{
    // 弛みを表現するための、下にずらす量
    public float slackAmount = 15f; 

    void Update()
    {
        // 【新しい入力システム方式】マウスの左クリック、またはスマホの画面タッチを検知
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            CutWire();
        }
    }

    void CutWire()
    {
        if (Camera.main == null) return;

        // 【新しい入力システム方式】現在のマウス/タッチ位置を取得
        Vector2 screenPosition = Pointer.current.position.ReadValue();
        
        // 画面の座標をゲーム内の2D座標に変換
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
        
        // レイキャストでコライダーを検知
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (hit.collider != null)
        {
            // クリックされた配線のLine Rendererを取得
            LineRenderer originalLine = hit.collider.GetComponent<LineRenderer>();

            if (originalLine != null)
            {
                // 弛んだ配線を生成する
                CreateSlackedWires(originalLine);

                // 元の配線を削除
                Destroy(hit.collider.gameObject);
            }
        }
    }

    // 元の配線を元に、弛んだ2本の配線を生成する関数
    void CreateSlackedWires(LineRenderer originalLine)
    {
        Color wireColor = originalLine.startColor;
        float wireWidth = originalLine.startWidth;
        Material wireMaterial = originalLine.sharedMaterial;

        Vector3[] originalPositions = new Vector3[4];
        if (originalLine.positionCount >= 4)
        {
            originalLine.GetPositions(originalPositions);
        }
        else
        {
            Debug.LogError($"{originalLine.gameObject.name} の Line Renderer の Positions (Size) を 4 に設定してください！");
            return;
        }

        // --- 1. 左側の弛んだ配線を作る ---
        GameObject leftWireObj = new GameObject(originalLine.name + "_Left");
        LineRenderer leftLine = leftWireObj.AddComponent<LineRenderer>();
        
        leftLine.sharedMaterial = wireMaterial;
        leftLine.startColor = leftLine.endColor = wireColor;
        leftLine.startWidth = leftLine.endWidth = wireWidth;
        leftLine.numCornerVertices = originalLine.numCornerVertices;
        leftLine.numCapVertices = originalLine.numCapVertices;

        leftLine.positionCount = 3;
        Vector3[] leftPositions = new Vector3[3];
        leftPositions[0] = originalPositions[0]; // 左端
        leftPositions[1] = originalPositions[1] - new Vector3(0, slackAmount, 0);
        leftPositions[2] = originalPositions[2] - new Vector3(0, slackAmount * 0.5f, 0); // 切断点
        leftLine.SetPositions(leftPositions);

        // --- 2. 右側の弛んだ配線を作る ---
        GameObject rightWireObj = new GameObject(originalLine.name + "_Right");
        LineRenderer rightLine = rightWireObj.AddComponent<LineRenderer>();

        rightLine.sharedMaterial = wireMaterial;
        rightLine.startColor = rightLine.endColor = wireColor;
        rightLine.startWidth = rightLine.endWidth = wireWidth;
        rightLine.numCornerVertices = originalLine.numCornerVertices;
        rightLine.numCapVertices = originalLine.numCapVertices;

        rightLine.positionCount = 3;
        Vector3[] rightPositions = new Vector3[3];
        rightPositions[0] = originalPositions[1] - new Vector3(0, slackAmount * 0.5f, 0); // 切断点
        rightPositions[1] = originalPositions[2] - new Vector3(0, slackAmount, 0);
        rightPositions[2] = originalPositions[3]; // 右端
        rightLine.SetPositions(rightPositions);
    }
}