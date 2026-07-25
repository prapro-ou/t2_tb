using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public static class TweenColorExtensions
{
    /// <summary>
    /// コンポーネontがDOColorに対応していればTweenを開始する（Tryパターン）
    /// </summary>
    public static bool TryDOColor(this Component component, Color targetColor, float duration, out Tweener tweener)
    {
        tweener = null;
        if (component == null) return false;

        // 1. UI系 (Image, Text, TextMeshProUGUI など)
        if (component is Graphic graphic)
        {
            tweener = graphic.DOColor(targetColor, duration);
            return true;
        }
        // 2. 2Dスプライト
        if (component is SpriteRenderer sprite)
        {
            tweener = sprite.DOColor(targetColor, duration);
            return true;
        }
        // 3. 3Dメッシュ・マテリアル系
        if (component is Renderer renderer)
        {
            tweener = renderer.material.DOColor(targetColor, duration);
            return true;
        }
        // 4. ライト（もし使う場合）
        if (component is Light light)
        {
            tweener = light.DOColor(targetColor, duration);
            return true;
        }

        // 対応する型がなかった場合
        return false;
    }
}