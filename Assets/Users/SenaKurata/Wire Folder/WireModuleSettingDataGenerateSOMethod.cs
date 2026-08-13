using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WireModuleSettingDataGenerate", menuName = "SO/ModuleSettingData/Wire")]
// TypeModuleSettingDataGenerateSOMethod<T> を継承する（Tには先ほど作った構造体を指定）
public class WireModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<WireModuleSettingData>
{
    [Header("配線の設定")]
    [SerializeField] private int totalWires = 4;

    // ボタンを押したときに正解データをランダム生成して渡す関数
    protected override WireModuleSettingData GeneratePacketType()
    {
        // 1. シャッフル用の配列を用意
        int[] sequence = new int[totalWires];
        List<int> numbers = new List<int>();
        for (int i = 0; i < totalWires; i++)
        {
            numbers.Add(i);
        }

        // 2. Fisher-Yates シャッフルで正解の順番を作る
        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = numbers[i];
            numbers[i] = numbers[randomIndex];
            numbers[randomIndex] = temp;
        }

        for (int i = 0; i < totalWires; i++)
        {
            sequence[i] = numbers[i];
        }

        Debug.Log($"【生成完了】今回の正解コード: {string.Join(", ", sequence)}");

        // 3. 作成したデータを構造体に詰めて返す
        return new WireModuleSettingData
        {
            totalWires = this.totalWires,
            correctSequence = sequence
        };
    }
}