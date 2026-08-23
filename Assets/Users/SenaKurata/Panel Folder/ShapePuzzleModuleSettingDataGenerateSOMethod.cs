using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ShapePuzzleModuleSettingDataGenerateSO", menuName = "Module/SettingGenerator/ShapePuzzle")]
public class ShapePuzzleModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<ShapePuzzleModuleSettingData>
{
    [Header("パズルの設定")]
    [Tooltip("パズルのピース数/要素数")]
    [Min(2)]
    [SerializeField] private int pieceCount = 4;

    protected override ShapePuzzleModuleSettingData GeneratePacketType()
    {
        int[] correct = Enumerable.Range(0, pieceCount).ToArray();
        ShapePuzzleUtility.Shuffle(correct);

        int[] initial = (int[])correct.Clone();
        int safetyCount = 0;
        do
        {
            ShapePuzzleUtility.Shuffle(initial);
            safetyCount++;
        } while (pieceCount > 1 && ShapePuzzleUtility.IsSequenceEqual(correct, initial) && safetyCount < 100);

        return new ShapePuzzleModuleSettingData
        {
            correctOrder = correct,
            initialOrder = initial
        };
    }
}