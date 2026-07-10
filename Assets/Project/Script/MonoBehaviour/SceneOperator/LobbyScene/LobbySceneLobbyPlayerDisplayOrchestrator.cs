using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
public class LobbySceneLobbyPlayerDisplayOrchestrator : MonoBehaviour
{
    [SerializeField] private GameObject _displayField;
    [SerializeField] private GameObject _displayPanel;

    public void SetPlayerName(Dictionary<ProductUserId, string> nameDictionary, ProductUserId hostID)
    {
        foreach (KeyValuePair<ProductUserId, string> name in nameDictionary)
        {
            GameObject panel = Instantiate(_displayPanel, Vector3.zero, Quaternion.identity, _displayField.transform);//パネル生成
            panel.GetComponentInChildren<LobbyUserDisplayOperator>()?.InitializeLobbyUserDisplay(name.Value, name.Key == hostID);
        }
    }
}