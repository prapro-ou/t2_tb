using UnityEngine;
using TMPro;
using OriginalNameSpace.EOSMethod.Lobby;
public class LobbySceneModuleCounterOrchestrator : MonoBehaviour
{
    [SerializeField] private EOSLobbyOperator _eosLobbyOperator;
    [SerializeField] private TMP_Text text;
    private int _moduleCounter = 0;
    public int ModuleCounter => CountCheck();
    [SerializeField] private AudioSource _audioSource;
    public void Initialize()
    {
        CountCheck();
    }

    public void OnAddClick()
    {
        _audioSource.Play();
        _moduleCounter++;
        CountCheck();
    }

    public void OnSubClick()
    {
        _audioSource.Play();
        _moduleCounter--;
        CountCheck();
    }

    private int CountCheck()
    {
        if (_moduleCounter < 1) _moduleCounter = 1;
        int memberCount = EOSLobbyMethod.GetLobbyMembers(_eosLobbyOperator.CurrentLobbyId).Count;
        if (_moduleCounter > memberCount * 7) _moduleCounter = memberCount * 7;
        text.text = _moduleCounter.ToString();
        return _moduleCounter;
    }
}
