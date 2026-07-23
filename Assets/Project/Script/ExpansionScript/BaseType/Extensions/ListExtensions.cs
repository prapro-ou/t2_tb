using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    // Listの拡張メソッドとして定義しておくと便利
    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            // 0 から i までのランダムなインデックスを選ぶ
            int randomIndex = Random.Range(0, i + 1);

            // 要素を入れ替える（スワップ）
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}