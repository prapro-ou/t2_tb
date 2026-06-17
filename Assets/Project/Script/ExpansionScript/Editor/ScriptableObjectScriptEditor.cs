using UnityEngine;
using UnityEditor;
using System;
using System.IO;
[CustomEditor(typeof(MonoScript), true)]
public class ScriptableObjectScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MonoScript script = (MonoScript)target;
        Type scriptType = script.GetClass();

        if (scriptType != null && scriptType.IsSubclassOf(typeof(ScriptableObject)))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ScriptableObject を作成", EditorStyles.boldLabel);

            if (GUILayout.Button($"Create {scriptType.Name} Asset"))
            {
                CreateScriptableObject(script, scriptType);
            }
        }
    }

    private void CreateScriptableObject(MonoScript script, Type type)
    {
        string scriptPath = AssetDatabase.GetAssetPath(script);
        string directoryPath = Path.GetDirectoryName(scriptPath);
        string assetPath = $"{directoryPath}/{type.Name}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        ScriptableObject obj = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(obj, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = obj;

        Debug.Log($"Created ScriptableObject: {assetPath}");
    }
}