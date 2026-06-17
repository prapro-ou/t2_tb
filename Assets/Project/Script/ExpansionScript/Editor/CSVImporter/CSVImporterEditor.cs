using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

// CSVImporterAndSettingsScriptObject のカスタムインスペクターを指定
[CustomEditor(typeof(CSVImporterAndSettingsScriptObject))]
public class CSVImporterEditor : Editor
{
    // フィールド情報をキャッシュするための静的辞書
    private static Dictionary<Type, Dictionary<string, FieldInfo>> s_FieldCache = null;

    // List / Dictionary の要素区切り文字（セミコロン）
    // 例: "A;B;C" → List<string> { "A", "B", "C" }
    private const char LIST_SEPARATOR = ';';

    // Dictionary のキーと値の区切り文字（コロン）
    // 例: "key1:val1;key2:val2" → Dictionary<string,string> { "key1"="val1", "key2"="val2" }
    private const char DICT_KEY_VALUE_SEPARATOR = ':';

    //<summary>
    // インスペクター上のGUIを描画する
    //</summary>
    public override void OnInspectorGUI()
    {
        // 設定アセットのデフォルトフィールドを描画
        DrawDefaultInspector();

        CSVImporterAndSettingsScriptObject settings = (CSVImporterAndSettingsScriptObject)target;

        GUILayout.Space(15);

        // SO生成ボタンを描画
        if (GUILayout.Button("設定に基づきScriptableObjectを作成", GUILayout.Height(35)))
        {
            // ボタンが押されたらCSVのインポートを実行
            RunImport(settings);
        }

        // 実行後、エディタに変更があったことを通知
        if (GUI.changed)
        {
            EditorUtility.SetDirty(settings);
        }
    }

    //<summary>
    // CSVをインポートするロジック
    // @param settings CSVImporterAndSettingsScriptObject
    //</summary>
    private void RunImport(CSVImporterAndSettingsScriptObject settings)
    {
        // ボタン側のSOの設定抜けチェック
        if (settings.targetCSVFile == null || string.IsNullOrEmpty(settings.targetScriptableObjectClassName))
        {
            Debug.LogError("CSVファイルまたはSOクラス名が設定されていません。");
            return;
        }

        //フォルダの存在チェック
        if (!Directory.Exists(settings.outputAssetFolderPath))
        {
            Debug.LogError("出力フォルダが存在しません。");
            return;
        }

        // CSVの読み込み
        List<Dictionary<string, string>> allData = ParseCsvText(settings.targetCSVFile.text);
        if (allData.Count <= 0)
        {
            Debug.LogWarning("CSVファイルにデータ行がありません。");
            return;
        }

        // 生成するSOの型を特定(リフレクションで取得)
        Type soType = GetScriptableObjectType(settings.targetScriptableObjectClassName);
        if (soType == null) return;

        // 処理結果を記録するためのリスト
        List<string> successfulAssets = new List<string>();
        // 失敗したアセット名とエラーメッセージをタプルで記録
        List<(string name, string message)> failedAssets = new List<(string, string)>();

        // データ行一行ごとにSOを生成
        for (int row = 0; row < allData.Count; row++)
        {
            Dictionary<string, string> rowData = allData[row];

            // 生成するアセット名とパスを決定
            string assetName = rowData.ContainsKey(settings.assetNameHeader) ? rowData[settings.assetNameHeader].Trim() : $"NewAsset_{row}";
            string assetPath = settings.outputAssetFolderPath.TrimEnd('/') + "/" + assetName + ".asset";

            try
            {
                // 既存のアセットを検索・ロード
                ScriptableObject existingAsset = AssetDatabase.LoadAssetAtPath(assetPath, soType) as ScriptableObject;

                ScriptableObject assetToUpdate;
                bool isNewAsset = false;

                if (existingAsset != null)
                {
                    // 既存アセットが見つかった場合：更新
                    assetToUpdate = existingAsset;
                }
                else
                {
                    // 既存アセットが見つからなかった場合：新規作成
                    assetToUpdate = CreateInstance(soType);
                    isNewAsset = true;
                }

                // データマッピングを実行
                MapDataToScriptableObject(assetToUpdate, rowData, settings.columnConfigs);

                // ディスクへの保存
                if (isNewAsset)
                {
                    AssetDatabase.CreateAsset(assetToUpdate, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(assetToUpdate);
                }

                successfulAssets.Add(assetName); // 成功リストに追加

            }
            catch (Exception ex)
            {
                // 例外が発生した場合、ログを出力し、失敗リストに追加して次の行へ
                string errorMessage = ex.Message;
                Debug.LogError($"アセット '{assetName}' の生成/更新に失敗しました。エラー: {errorMessage}");
                failedAssets.Add((assetName, errorMessage));
            }
        }

        // エディタの更新
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 処理結果の報告
        if (failedAssets.Count > 0)
        {
            Debug.LogError($"===================================================");
            Debug.LogError($"--- アセットインポートが完了しました (エラーあり))---");
            Debug.LogError($"総データ件数: {allData.Count}件, 成功: {successfulAssets.Count}件, 失敗: {failedAssets.Count}件");
            Debug.LogError($"---------------------------------------------------");
            foreach (var fail in failedAssets)
            {
                Debug.LogError($"[失敗] {fail.name}: {fail.message}");
            }
            Debug.LogError($"===================================================");
        }
        else
        {
            // 全て成功した場合
            Debug.Log($"{settings.targetScriptableObjectClassName} のアセット生成がエラー無しで完了しました！ ({allData.Count}件)");
        }
    }

    //<summary>
    // フィールド情報をキャッシュから取得するか、リフレクションで新規取得しキャッシュする
    //</summary>
    private FieldInfo GetCachedField(Type type, string fieldName)
    {
        // 初期化の実行
        if (s_FieldCache == null)
        {
            s_FieldCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        }
        // トップレベルの型キャッシュをチェック
        if (!s_FieldCache.TryGetValue(type, out var typeCache))
        {
            typeCache = new Dictionary<string, FieldInfo>();
            s_FieldCache[type] = typeCache;
        }

        // フィールド名のキャッシュをチェック
        if (typeCache.TryGetValue(fieldName, out var fieldInfo))
        {
            return fieldInfo;
        }

        // キャッシュにない場合、リフレクションで取得しキャッシュに保存
        FieldInfo foundField = null;
        Type currentT = type;

        while (currentT != null && foundField == null)
        {
            foundField = currentT.GetField(fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            currentT = currentT.BaseType;
        }

        typeCache[fieldName] = foundField;
        return foundField;
    }

    //<summary>
    // SOの型を特定するメソッド
    // @param className SOのクラス名
    //</summary>
    private Type GetScriptableObjectType(string className)
    {
        // AppDomain内の全てのアセンブリからクラス名を検索
        Type type = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(className);
            if (type != null) break;
        }

        if (type == null || !type.IsSubclassOf(typeof(ScriptableObject)))
        {
            Debug.LogError($"クラス名 '{className}' が見つからないか、ScriptableObjectを継承していません。");
            return null;
        }
        return type;
    }

    //<summary>
    // CSVデータをSOにマッピングするメソッド
    // @param asset SOのインスタンス
    // @param rowData CSVデータ行
    // @param configs マッピング設定
    //</summary>
    private bool MapDataToScriptableObject(ScriptableObject asset, Dictionary<string, string> rowData, List<CSVColumnConfig> configs)
    {
        foreach (var config in configs)
        {
            // CSVの値を取得
            if (!rowData.TryGetValue(config.csvColumnHeaderName, out string csvValue))
            {
                Debug.LogWarning($"CSVにヘッダー '{config.csvColumnHeaderName}' が見つかりません");
                continue;
            }
            csvValue = csvValue.Trim();
            if (string.IsNullOrEmpty(csvValue)) continue;

            // ネスト構造を考慮してフィールドを再帰的に特定し、値を設定
            SetFieldValue(asset, config.targetFieldName, csvValue, config.assetPathPrefix);
        }
        return true;
    }

    // <summary>
    // SOのフィールドに値を設定するメソッド
    // @param targetObject SOのインスタンス
    // @param fullFieldName フィールド名(例: "targetStats.addScore")
    // @param csvValue CSVの値
    // @param assetPathPrefix SOのアセットパスの先頭部分
    // </summary>
    private void SetFieldValue(object targetObject, string fullFieldName, string csvValue, string assetPathPrefix)
    {
        // フィールド名をドット(.)で分割
        string[] fieldNames = fullFieldName.Split('.');

        // 最初のオブジェクトと型を取得
        Type currentType = targetObject.GetType();
        object currentObject = targetObject;

        // Stackに格納する情報:(書き戻す FieldInfo, 書き戻す 親オブジェクト)
        Stack<(FieldInfo fieldInfo, object parentObject)> writeBackChain = new Stack<(FieldInfo, object)>();

        // フィールドを再帰的に設定
        for (int i = 0; i < fieldNames.Length; i++)
        {
            // フィールド名・型を取得
            string fieldName = fieldNames[i];
            FieldInfo field = GetCachedField(currentType, fieldName);

            if (field == null)
            {
                Debug.LogWarning($"フィールド '{fieldName}' が型 '{currentType.Name}' に見つかりませんでした。スキップします。");
                return;
            }

            // 終端フィールドではない場合
            if (i < fieldNames.Length - 1)
            {
                object nextObject = field.GetValue(currentObject);

                // 階層がクラスでnullの場合、動的にインスタンスを生成
                if (nextObject == null && field.FieldType.IsClass)
                {
                    // Unityオブジェクトでないことの安全性をチェック
                    if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        Debug.LogError($"アセット'{targetObject.GetType().Name}'のネストされたフィールド '{fullFieldName}' の途中にUnityオブジェクト({field.FieldType.Name})が含まれており、参照がnullでした。この中間パスで新規作成は許可されていません。");
                        return;
                    }

                    // Classのインスタンスを生成し、親オブジェクトのフィールドに設定
                    nextObject = Activator.CreateInstance(field.FieldType);
                    field.SetValue(currentObject, nextObject); // 親オブジェクトへの参照を確立
                }

                // 現在のフィールド情報と親オブジェクトをスタックに保存
                writeBackChain.Push((field, currentObject));

                // 次階層へ移動
                currentObject = nextObject;
                currentType = field.FieldType;
            }
            else // 終端フィールドの場合
            {
                // 終端フィールドの情報と、その親オブジェクトをスタックにプッシュしてループを終了
                writeBackChain.Push((field, currentObject));
                break;
            }
        }

        // 終端フィールドの情報を取り出す
        (FieldInfo finalFieldInfo, object finalParentObject) = writeBackChain.Pop();

        // CSVの値を目標の型に変換
        object settingObject = ConvertValue(csvValue, finalFieldInfo.FieldType, assetPathPrefix);

        // 終端フィールドに値を設定
        finalFieldInfo.SetValue(finalParentObject, settingObject);

        // 終端フィールドを得たネストを次の親へ書き戻すためのオブジェクト
        object objectToSet = finalParentObject;

        while (writeBackChain.Count > 0)
        {
            // スタックから一つ上の階層の情報を取得
            (FieldInfo fieldInfoToFix, object objectToFix) = writeBackChain.Pop();

            // 変更された子オブジェクト(objectToSet)を、親オブジェクト(objectToFix)のフィールドに上書き
            fieldInfoToFix.SetValue(objectToFix, objectToSet);

            // 今更新した親オブジェクトを次の書き戻しの対象にする
            objectToSet = objectToFix;
        }
    }

    // <summary>
    // 文字列データを目標の型に変換
    // List<T> / Dictionary<TKey,TValue> にも対応
    // </summary>
    private object ConvertValue(string value, Type targetFieldType, string assetPathPrefix)
    {
        // ---------------------------------------------------------------
        // 1. List<T> の処理
        //    CSV書式: "A;B;C"  →  List<T> { A, B, C }
        // ---------------------------------------------------------------
        if (targetFieldType.IsGenericType && targetFieldType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return ConvertToList(value, targetFieldType, assetPathPrefix);
        }

        // ---------------------------------------------------------------
        // 2. Dictionary<TKey, TValue> の処理
        //    CSV書式: "key1:val1;key2:val2"  →  Dictionary<TKey,TValue>
        // ---------------------------------------------------------------
        if (targetFieldType.IsGenericType && targetFieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return ConvertToDictionary(value, targetFieldType, assetPathPrefix);
        }

        // ---------------------------------------------------------------
        // 3. Enum の処理（[Flags] 属性によるフラグ合成に対応）
        //    通常 Enum : "Fire"
        //    [Flags] Enum: "Fire,Ice,Thunder"  ← カンマ区切りで OR 合成
        // ---------------------------------------------------------------
        if (targetFieldType.IsEnum)
        {
            return ConvertEnum(value, targetFieldType);
        }

        // ---------------------------------------------------------------
        // 4. UnityObject (アセット参照) の処理
        // ---------------------------------------------------------------
        if (targetFieldType.IsSubclassOf(typeof(UnityEngine.Object)) || targetFieldType == typeof(UnityEngine.Object))
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            string assetExtension = ".asset";
            if (targetFieldType == typeof(Sprite) ||
                targetFieldType == typeof(Texture2D) ||
                targetFieldType == typeof(GameObject))
            {
                assetExtension = "";
            }

            string fullPath = assetPathPrefix.TrimEnd('/') + "/" + value + assetExtension;
            var loadedAsset = AssetDatabase.LoadAssetAtPath(fullPath, targetFieldType);

            if (loadedAsset == null)
            {
                Debug.LogWarning($"アセット参照型 ({targetFieldType.Name}) の値 '{value}' に対応するアセットをパス '{fullPath}' でロードできませんでした。");
            }
            return loadedAsset;
        }

        // ---------------------------------------------------------------
        // 5. プリミティブ型 / string / struct の処理
        // ---------------------------------------------------------------
        return ConvertPrimitive(value, targetFieldType);
    }

    // <summary>
    // Enum 型（[Flags] 対応）を文字列から変換する
    //
    // 通常 Enum  : "Fire"               → MyEnum.Fire
    // [Flags] Enum: "Fire,Ice,Thunder"  → MyFlags.Fire | MyFlags.Ice | MyFlags.Thunder
    //
    // [Flags] かどうかは Attribute で自動判定するため、
    // CSV 側はカンマ区切りで書くだけでよい。
    //   ・カンマが含まれない場合は Enum.Parse に直接委譲（通常 Enum でも安全）
    //   ・カンマが含まれる場合は [Flags] とみなして各トークンを OR 合成する
    // </summary>
    private object ConvertEnum(string value, Type enumType)
    {
        // カンマが含まれない場合 → 通常の単一値パース（[Flags] の数値表現も含む）
        if (!value.Contains(','))
        {
            try
            {
                return Enum.Parse(enumType, value.Trim(), ignoreCase: true);
            }
            catch
            {
                Debug.LogError($"Enum '{enumType.Name}' の値 '{value}' を変換できませんでした。");
                return null;
            }
        }

        // カンマ区切りのフラグ合成処理
        // [Flags] 属性の有無をチェックして警告を出す（動作はさせる）
        bool hasFlagsAttr = enumType.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Length > 0;
        if (!hasFlagsAttr)
        {
            Debug.LogWarning($"Enum '{enumType.Name}' に [Flags] 属性がありませんが、カンマ区切りの値 '{value}' が指定されました。OR 合成を試みます。");
        }

        // 各トークンをパースして long レベルで OR 合成
        // Enum の基底型が long のケースも考慮して long で計算する
        long combined = 0;
        string[] tokens = value.Split(',');
        foreach (string token in tokens)
        {
            string trimmed = token.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            try
            {
                object parsed = Enum.Parse(enumType, trimmed, ignoreCase: true);
                combined |= Convert.ToInt64(parsed);
            }
            catch
            {
                Debug.LogError($"[Flags] Enum '{enumType.Name}' のフラグ値 '{trimmed}' を変換できませんでした。このフラグはスキップされます。");
            }
        }

        // long → Enum の基底型へ変換してから Enum にキャスト
        try
        {
            object underlyingValue = Convert.ChangeType(combined, Enum.GetUnderlyingType(enumType));
            return Enum.ToObject(enumType, underlyingValue);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Flags] Enum '{enumType.Name}' の合成値 '{combined}' を最終変換できませんでした: {ex.Message}");
            return null;
        }
    }

    // <summary>
    // CSV文字列を List<T> に変換する
    // CSV書式: "elem1;elem2;elem3"
    // 空文字列の要素はスキップされる
    // </summary>
    private object ConvertToList(string value, Type listType, string assetPathPrefix)
    {
        // List<T> の要素型を取得
        Type elementType = listType.GetGenericArguments()[0];

        // IList として操作できるインスタンスを生成
        var list = (System.Collections.IList)Activator.CreateInstance(listType);

        // セミコロンで分割して各要素を変換
        string[] tokens = value.Split(LIST_SEPARATOR);
        foreach (string token in tokens)
        {
            string trimmed = token.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            object convertedElement = ConvertValue(trimmed, elementType, assetPathPrefix);
            if (convertedElement != null)
            {
                list.Add(convertedElement);
            }
            else
            {
                // null 許容型でない場合でも Add しない（警告は ConvertValue 内で出力済み）
                Debug.LogWarning($"List<{elementType.Name}> の要素 '{trimmed}' の変換に失敗したため、この要素はスキップされます。");
            }
        }

        return list;
    }

    // <summary>
    // CSV文字列を Dictionary<TKey, TValue> に変換する
    // CSV書式: "key1:val1;key2:val2"
    // 同じキーが複数ある場合は後勝ちで上書きされる
    // </summary>
    private object ConvertToDictionary(string value, Type dictType, string assetPathPrefix)
    {
        // Dictionary<TKey, TValue> のキー型・値型を取得
        Type[] genericArgs = dictType.GetGenericArguments();
        Type keyType = genericArgs[0];
        Type valueType = genericArgs[1];

        // IDictionary として操作できるインスタンスを生成
        var dict = (System.Collections.IDictionary)Activator.CreateInstance(dictType);

        // まずセミコロンでペアに分割
        string[] pairs = value.Split(LIST_SEPARATOR);
        foreach (string pair in pairs)
        {
            string trimmedPair = pair.Trim();
            if (string.IsNullOrEmpty(trimmedPair)) continue;

            // コロンでキーと値に分割（最初の1つだけを区切りとして扱う）
            int separatorIndex = trimmedPair.IndexOf(DICT_KEY_VALUE_SEPARATOR);
            if (separatorIndex < 0)
            {
                Debug.LogWarning($"Dictionary のペア '{trimmedPair}' に区切り文字 '{DICT_KEY_VALUE_SEPARATOR}' が見つかりません。このペアはスキップされます。");
                continue;
            }

            string rawKey = trimmedPair.Substring(0, separatorIndex).Trim();
            string rawValue = trimmedPair.Substring(separatorIndex + 1).Trim();

            // キーと値をそれぞれ型変換
            object convertedKey = ConvertValue(rawKey, keyType, assetPathPrefix);
            object convertedValue = ConvertValue(rawValue, valueType, assetPathPrefix);

            if (convertedKey == null)
            {
                Debug.LogWarning($"Dictionary のキー '{rawKey}' の変換に失敗しました。このペアはスキップされます。");
                continue;
            }

            // 重複キーは後勝ちで上書き
            dict[convertedKey] = convertedValue;
        }

        return dict;
    }

    // <summary>
    // プリミティブ型 / string / struct を文字列から変換する
    // </summary>
    private object ConvertPrimitive(string value, Type targetFieldType)
    {
        try
        {
            TypeCode typeCode = Type.GetTypeCode(targetFieldType);

            switch (typeCode)
            {
                case TypeCode.String:
                    return value;
                case TypeCode.Int32:
                    if (int.TryParse(value, out int i)) return i;
                    break;
                case TypeCode.Single:
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
                    break;
                case TypeCode.Double:
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                    break;
                case TypeCode.Boolean:
                    if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1") return true;
                    if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0") return false;
                    break;
                case TypeCode.Int64:
                    if (long.TryParse(value, out long l)) return l;
                    break;
                case TypeCode.Int16:
                    if (short.TryParse(value, out short s)) return s;
                    break;
                case TypeCode.Byte:
                    if (byte.TryParse(value, out byte b)) return b;
                    break;
                case TypeCode.Char:
                    if (value.Length == 1) return value[0];
                    break;
                default:
                    Debug.LogWarning($"未対応の型 '{targetFieldType.Name}' の値 '{value}' を変換できません。");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"<color=yellow>[変換失敗]</color> 値 '{value}' を型 '{targetFieldType.Name}' に変換できませんでした。");
            Debug.LogError($"型変換中にエラーが発生しました: {ex.Message}");
        }
        return null;
    }

    // <summary>
    // CSVテキストをパースし、ヘッダーをキーとした辞書リストとして返す
    // </summary>
    private static List<Dictionary<string, string>> ParseCsvText(string csvText)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

        // 改行コードを統一
        string normalizedText = csvText.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] lines = normalizedText.Split('\n');

        if (lines.Length <= 1) return result;

        // ヘッダー行をパース
        string[] headers = ParseCsvLine(lines[0]);
        if (headers == null || headers.Length == 0) return result;

        // データ行を処理
        for (int row = 1; row < lines.Length; row++)
        {
            string line = lines[row];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = ParseCsvLine(line);
            if (values == null)
            {
                Debug.LogError($"CSVデータ行 {row + 1} のパースに失敗しました。この行はスキップされます。");
                continue;
            }

            var rowDict = new Dictionary<string, string>();
            for (int column = 0; column < headers.Length; column++)
            {
                // 値の配列がヘッダー数より少ない場合でもエラーを防ぐ
                string value = (column < values.Length) ? values[column] : string.Empty;

                // ヘッダー名をキーとして辞書に追加
                rowDict[headers[column]] = value.Trim(); // 値の前後空白をトリム
            }

            result.Add(rowDict);
        }

        return result;
    }

    // <summary>
    // 1行のCSVテキストをパースする（二重引用符によるエスケープ対応）
    // </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuote = false; // 二重引用符の内側にいるか

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (i < line.Length - 1 && line[i + 1] == '"')
                {
                    // "" (二重引用符のエスケープ) の場合
                    sb.Append('"');
                    i++; // 次の文字をスキップ
                }
                else
                {
                    // 通常の引用符の開始または終了
                    inQuote = !inQuote;
                }
            }
            else if (c == ',')
            {
                if (inQuote)
                {
                    // 引用符内にあるカンマはそのまま値に含める
                    sb.Append(c);
                }
                else
                {
                    // 引用符の外にあるカンマは区切り文字
                    fields.Add(sb.ToString().Trim()); // フィールドを確定
                    sb.Clear();
                }
            }
            else
            {
                // その他の文字はそのまま追加
                sb.Append(c);
            }
        }

        // 最後のフィールドを追加
        fields.Add(sb.ToString().Trim());

        return fields.ToArray();
    }
}