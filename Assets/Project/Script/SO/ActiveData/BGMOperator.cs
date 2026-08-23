using UnityEngine;

public class BGMOperator : ScriptableObject
{
    private AudioSource _audioSource;

    public void Initialize(AudioSource audioSource) => _audioSource = audioSource;

    public void SetPlay(AudioClip clip)
    {
        if (_audioSource == null || clip == null || _audioSource.clip == clip) return;
        _audioSource.clip = clip;
        _audioSource.Play();
    }
}