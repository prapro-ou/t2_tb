using UnityEngine;

public class CoreSceneBGMOperator : MonoBehaviour
{
    [SerializeField] AudioSource _bgmAudioSource;
    [SerializeField] BGMOperator _bgmOperator;
    public void Initialize()
    {
        _bgmOperator.Initialize(_bgmAudioSource);
    }
}
