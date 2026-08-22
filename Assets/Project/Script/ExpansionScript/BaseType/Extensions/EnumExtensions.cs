using System;
using System.Collections.Generic;
using System.Linq;
public static class EnumExtensions
{

    /// <summary>
    /// ランダムな列挙型の値を取得する拡張メソッド
    /// </summary>
    public static T GetRandom<T>() where T : Enum
    {
        T[] values = (T[])Enum.GetValues(typeof(T));
        int randomIndex = UnityEngine.Random.Range(0, values.Length);
        return values[randomIndex];
    }

    ///<summary>
    /// 列挙型をリストに変換する拡張メソッド
    /// </summary>
    public static List<T> ToList<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }

    /// <summary>
    /// 指定された数のランダムな列挙型の値を取得する拡張メソッド
    /// </summary>
    public static List<T> GetUniqueEnumValues<T>(int n) where T : Enum
    {
        // 1. 古い環境互換の記述で全要素を取得
        T[] allValues = Enum.GetValues(typeof(T)).Cast<T>().ToArray();

        // 2. 要素数チェック
        if (n > allValues.Length)
        {
            throw new ArgumentException($"指定された数({n})が、列挙型の要素数({allValues.Length})を超えています。");
        }

        // 3. ランダムインスタンスを都度生成してシャッフル
        Random rand = new Random();
        return allValues
            .OrderBy(_ => rand.Next()) // ここを rand.Next() に修正しました
            .Take(n)
            .ToList();
    }
}