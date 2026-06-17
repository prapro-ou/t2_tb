using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// 全ての子オブジェクトを破棄する
    /// </summary>
    public static void DestroyAllChildren(this Transform parent)
    {
        foreach (Transform child in parent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}