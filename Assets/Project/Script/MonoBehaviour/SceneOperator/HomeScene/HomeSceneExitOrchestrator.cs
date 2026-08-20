using UnityEngine;

public class HomeSceneExitOrchestrator : MonoBehaviour
{
    public void OnClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }
}
