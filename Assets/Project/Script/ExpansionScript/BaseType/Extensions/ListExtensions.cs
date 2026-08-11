using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ListExtensions
{
    public static List<T> Shuffle<T>(this List<T> list)
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
        return list;
    }


    public static T GetRandom<T>(this List<T> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    public static List<T> GetRandomList<T>(this List<T> sourceList, int count)
    {
        List<T> copy = new List<T>(sourceList);
        copy = copy.Shuffle();
        return copy.GetRange(0, Mathf.Min(count, copy.Count));
    }
}