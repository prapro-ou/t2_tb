using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputActionAsset用のユーティリティクラス。
/// </summary>
public static class InputActionUtility
{
    /// <summary>
    /// 指定されたアクションマップのみを有効化し、他を無効化します。
    /// </summary>
    /// <param name="inputActionAsset">対象のInputActionAsset</param>
    /// <param name="targetMapName">有効化したいアクションマップの名前</param>
    public static void EnableOnlyTargetMap(InputActionAsset inputActionAsset, string targetMapName)
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("InputActionAssetがnullです");
            return;
        }

        foreach (var actionMap in inputActionAsset.actionMaps)
        {
            if (actionMap.name == targetMapName)
            {
                actionMap.Enable();
                Debug.Log($"'{actionMap.name}' を有効化しました。");
            }
            else
            {
                actionMap.Disable();
                Debug.Log($"'{actionMap.name}' を無効化しました。");
            }
        }
    }

    /// <summary>
    /// 指定されたインプットアクションアセットのアクションを全て無効化します。
    /// </summary>
    /// <param name="inputActionAsset">対象のInputActionAsset</param>
    public static void DisableAllMap(InputActionAsset inputActionAsset)
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("InputActionAssetがnullです");
            return;
        }

        foreach (var actionMap in inputActionAsset.actionMaps)
        {
            actionMap.Disable();
            Debug.Log($"'{actionMap.name}' を無効化しました。");
        }
    }
}