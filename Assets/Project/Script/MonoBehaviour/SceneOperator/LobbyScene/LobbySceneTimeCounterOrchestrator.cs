using System;
using UnityEngine;
using TMPro;
public class LobbySceneTimeCounterOrchestrator : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    private int _timeCounter = 0;
    public int TimeCounter => _timeCounter;
    [SerializeField] private AudioSource _audioSource;
    public void Initialize()
    {
        TimeCheck();
    }

    public void OnAddClick()
    {
        _audioSource.Play();
        _timeCounter++;
        TimeCheck();
    }

    public void OnSubClick()
    {
        _audioSource.Play();
        _timeCounter--;
        TimeCheck();
    }

    private int TimeCheck()
    {
        if (_timeCounter < 1) _timeCounter = 1;
        if (_timeCounter > 20) _timeCounter = 20;
        TimeSpan timeSpan = TimeSpan.FromSeconds(_timeCounter * 15);
        text.text = $"<mspace=0.55em>{timeSpan.ToString(@"mm\:ss")}</mspace=0.55em>";
        return _timeCounter;
    }
}