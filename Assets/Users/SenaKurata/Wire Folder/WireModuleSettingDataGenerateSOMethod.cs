using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "WireModuleSettingDataGenerate", menuName = "SO/ModuleSettingData/Wire")]
public class WireModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<WireModuleSettingData>
{
    [Header("配線の設定")]
    [SerializeField] private int totalWires = 4;

    // 正解データをランダム生成して渡す関数
    protected override WireModuleSettingData GeneratePacketType()
    {
        // 0 〜 (totalWires - 1) のインデックスリストを作成
        List<int> numbers = Enumerable.Range(0, totalWires).ToList();

        // Fisher-Yates シャッフルで正解の順番を作成
        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = numbers[i];
            numbers[i] = numbers[randomIndex];
            numbers[randomIndex] = temp;
        }

        int[] sequence = numbers.ToArray();

        Debug.Log($"【生成完了】今回の正解コード: {string.Join(", ", sequence)}");

        return new WireModuleSettingData
        {
            totalWires = this.totalWires,
            correctSequence = sequence
        };
    }
}