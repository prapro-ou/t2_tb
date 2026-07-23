using System;
using System.Collections.Generic;
using System.Linq;
public static class EnumExtensions
{
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