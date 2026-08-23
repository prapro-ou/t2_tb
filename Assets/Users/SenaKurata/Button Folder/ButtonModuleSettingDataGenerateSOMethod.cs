using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ButtonModuleSettingDataGenerate", menuName = "SO/ModuleSettingData/Button")]
public class ButtonModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<ButtonModuleSettingData>
{
    [Header("ボタンの設定")]
    [SerializeField] private int totalButtons = 4;

    // 正解データをランダム生成して渡す関数
    protected override ButtonModuleSettingData GeneratePacketType()
    {
        // 0 〜 (totalButtons - 1) のインデックスリストを作成
        List<int> numbers = Enumerable.Range(0, totalButtons).ToList();

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

        return new ButtonModuleSettingData
        {
            totalButtons = this.totalButtons,
            correctSequence = sequence
        };
    }
}