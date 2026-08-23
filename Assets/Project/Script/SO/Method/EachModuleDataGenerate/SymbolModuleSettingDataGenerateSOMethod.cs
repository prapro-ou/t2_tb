using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SymbolModuleSettingDataGenerateSOMethod
    : TypeModuleSettingDataGenerateSOMethod<SymbolModuleSettingData>
{
    protected override SymbolModuleSettingData GeneratePacketType()
    {
        List<int> symbolIndices = Enumerable.Range(0, 10).ToList();

        for (int i = symbolIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = symbolIndices[i];
            symbolIndices[i] = symbolIndices[randomIndex];
            symbolIndices[randomIndex] = temp;
        }

        symbolIndices = symbolIndices.Take(4).ToList();

        List<int> correctOrder = new List<int> { 1, 2, 3, 4 };

        for (int i = correctOrder.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = correctOrder[i];
            correctOrder[i] = correctOrder[randomIndex];
            correctOrder[randomIndex] = temp;
        }

        return new SymbolModuleSettingData
        {
            SymbolIndices = symbolIndices,
            CorrectOrder = correctOrder
        };
    }
}
