using UnityEngine;

// 2. TypeModuleSettingDataGenerateSOMethod<T> を継承したSO生成用クラス
[CreateAssetMenu(fileName = "ShapePuzzleModuleSettingDataGenerateSO", menuName = "Module/SettingGenerator/ShapePuzzle")]
public class ShapePuzzleModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<ShapePuzzleModuleSettingData>
{
    [Header("パズルの初期設定")]
    [SerializeField] private int[] correctOrder = new int[4] { 0, 1, 2, 3 };
    [SerializeField] private int[] initialOrder = new int[4] { 3, 2, 1, 0 };

    protected override ShapePuzzleModuleSettingData GeneratePacketType()
    {
        return new ShapePuzzleModuleSettingData
        {
            correctOrder = this.correctOrder,
            initialOrder = this.initialOrder
        };
    }
}