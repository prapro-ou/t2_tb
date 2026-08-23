using UnityEngine;

public class HomeSceneExitOrchestrator : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    public void OnClick()
    {
        _audioSource.Play();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }
}
