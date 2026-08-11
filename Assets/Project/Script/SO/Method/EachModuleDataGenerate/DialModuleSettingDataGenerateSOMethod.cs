using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
public class DialModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<DialModuleSettingData>
{
    protected override DialModuleSettingData GeneratePacketType()
    {
        Debug.Log("DialModuleSettingDataGenerateSOMethod");
        DialModuleSettingData dialModuleSettingData = new()
        {
            MarkIndexList = new(),
            HaveMarkerModule = new()
        };

        foreach (ModuleVersionEnum item in new List<ModuleVersionEnum>() { ModuleVersionEnum.A, ModuleVersionEnum.B })
        {
            List<int> markerIndexList = Enumerable.Range(0, 12).ToList();
            List<int> pairMarkerList = markerIndexList.GetRandomList(3);
            dialModuleSettingData.MarkIndexList.Add(item, pairMarkerList);
            if (item == ModuleVersionEnum.A)
            {
                dialModuleSettingData.HaveMarkerModule.Add(item, ModuleVersionEnum.B);
            }
            else if (item == ModuleVersionEnum.B)
            {
                dialModuleSettingData.HaveMarkerModule.Add(item, ModuleVersionEnum.A);
            }
        }
        return dialModuleSettingData;
    }
}