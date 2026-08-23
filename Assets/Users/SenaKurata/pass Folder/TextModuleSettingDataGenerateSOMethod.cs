using System.Collections.Generic;
using UnityEngine;

// TypeModuleSettingDataGenerateSOMethod<T> を継承
public class TextModuleSettingDataGenerateSOMethod : TypeModuleSettingDataGenerateSOMethod<TextModuleSettingData>
{
    [Header("文字列生成の設定")]
    [Tooltip("生成に使用する文字の候補")]
    [SerializeField] private string characterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Tooltip("生成する文字数")]
    [SerializeField] private int textLength = 6;

    // ボタンなどを押したときにランダムな文字列を生成して渡す関数
    protected override TextModuleSettingData GeneratePacketType()
    {
        if (string.IsNullOrEmpty(characterPool))
        {
            Debug.LogError("【生成エラー】characterPool が空です。文字列を設定してください。");
            return new TextModuleSettingData { targetText = "ERROR" };
        }

        // 指定された長さのランダムな文字列を生成
        char[] result = new char[textLength];
        for (int i = 0; i < textLength; i++)
        {
            int randomIndex = Random.Range(0, characterPool.Length);
            result[i] = characterPool[randomIndex];
        }

        string generatedText = new string(result);

        Debug.Log($"【生成完了】今回の正解テキスト: {generatedText}");

        // 構造体に詰めて返す
        return new TextModuleSettingData
        {
            targetText = generatedText
        };
    }
}