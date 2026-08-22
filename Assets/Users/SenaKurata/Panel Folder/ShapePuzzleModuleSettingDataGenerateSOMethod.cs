using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ShapePuzzleModuleSettingDataGenerateSO", menuName = "Module/SettingGenerator/ShapePuzzle")]
public class ShapePuzzleModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<ShapePuzzleModuleSettingData>
{
    [Header("パズルの設定")]
    [Tooltip("パズルのピース数/要素数")]
    [SerializeField] private int pieceCount = 4;

    protected override ShapePuzzleModuleSettingData GeneratePacketType()
    {
        int[] correct = Enumerable.Range(0, pieceCount).ToArray();
        Shuffle(correct);

        int[] initial = (int[])correct.Clone();
        do
        {
            Shuffle(initial);
        } while (pieceCount > 1 && IsSequenceEqual(correct, initial));

        return new ShapePuzzleModuleSettingData
        {
            correctOrder = correct,
            initialOrder = initial
        };
    }

    private void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    private bool IsSequenceEqual(int[] a, int[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }
}