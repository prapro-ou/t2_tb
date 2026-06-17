using UnityEngine;
using System.Collections.Generic;
// CSVの一つの列とSOの一つのフィールドを対応させる設定
[System.Serializable]
public class CSVColumnConfig
{
    // CSVのヘッダー名
    public string csvColumnHeaderName;

    // 変換先のSOのフィールド名.ネスト構造は「構造体名.フィールド名」で指定
    public string targetFieldName;

    // 特定の型をロードする際のパスのプレフィックス
    public string assetPathPrefix;
}

// ボタン役のScriptableObjectを生成するための設定
public class CSVImporterAndSettingsScriptObject : ScriptableObject
{
    [Header("--- データソースと出力設定 ---")]
    // 読み込むCSVファイル名
    public TextAsset targetCSVFile;

    // 生成するSOのスクリプト名
    public string targetScriptableObjectClassName;

    // 生成したアセットを保存するパス
    public string outputAssetFolderPath = "Assets/GameData/Assets/";

    [Header("--- ヘッダーとマッピング設定 ---")]
    // CSVのどの列を生成アセットのファイル名として使用するか
    public string assetNameHeader = "ScriptableObjectName";

    // CSVの列とSOのフィールドのマッピング設定リスト
    public List<CSVColumnConfig> columnConfigs = new List<CSVColumnConfig>();
}