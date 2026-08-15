using UnityEngine;

public class StopwatchOperatorController : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField]
    private StopwatchModuleToolsOrchestrator _tools;

    // 現在タイマーが動いているか
    private bool _isRunning = false;

    /// <summary>
    /// START / STOPボタン
    /// </summary>
    public void PressActionButton()
    {
        if (_tools == null)
        {
            Debug.LogError(
                "StopwatchModuleToolsOrchestrator is not assigned."
            );
            return;
        }

        ModuleVersionEnum myVersion =
            _tools.GetMyModuleVersion();

        if (!_isRunning)
        {
            // START
            _tools.SendAction(
                myVersion,
                true
            );

            _isRunning = true;

            Debug.Log("START packet sent.");
        }
        else
        {
            // STOP
            _tools.SendAction(
                myVersion,
                false
            );

            _isRunning = false;

            Debug.Log("STOP packet sent.");
        }
    }
}