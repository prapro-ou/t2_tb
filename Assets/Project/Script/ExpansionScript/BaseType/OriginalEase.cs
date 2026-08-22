using UnityEngine;

/// <summary>
/// プロジェクト全体で使い回す汎用AnimationCurveライブラリ
/// </summary>
public static class OriginalEase
{

    public static AnimationCurve BackOutQuad()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.5f),
            new Keyframe(0.7f, 1.12f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
    }

    public static AnimationCurve SpringUI()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3f),
            new Keyframe(0.5f, 1.15f, 0f, 0f),
            new Keyframe(0.9f, 0.95f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
    }
}