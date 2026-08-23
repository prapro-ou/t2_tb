using System.Collections;
using UnityEngine;
using TMPro;

public class TimerGameManager : MonoBehaviour
{
    [Header("Orchestrator")]
    [SerializeField] private StopwatchToolsOrchestrator toolsOrchestrator;

    [Header("Manager")]
    public TimerManager timerManager;
    public GameData gameData;

    [Header("Board")]
    public BoardController timerInstructionBoard;

    [Header("UI")]
    public TMP_Text instructionText;

    private StopwatchModuleSettingData currentData;
    private string listenerUuid;

    public void OnInitialize(StopwatchModuleSettingData data)
    {
        this.currentData = data;

        if (gameData != null)
        {
            gameData.Initialize(data);
            gameData.GenerateQuestions();
        }

        ShowCurrentQuestion();

        if (toolsOrchestrator != null)
        {
            listenerUuid = toolsOrchestrator.ModuleDataRegisterListener<StopwatchModuleActionPacket>((version, packet) =>
            {
                if (packet.ActionType == StopwatchActionType.ToggleTimer)
                {
                    if (packet.IsRunning) ReceiveStart();
                    else
                    {
                        ReceiveStop();
                        CheckAnswer();
                    }
                }
            });
        }
    }

private void ShowCurrentQuestion(bool isRetry = false)
{
    float target = gameData != null ? gameData.GetCurrentTarget() : currentData.minTargetTime;
    float tol = currentData.tolerance;

    ShowQuestion(target, tol, isRetry);
}

private void CheckAnswer()
{
    float measuredTime = GetMeasuredTime();
    float target = gameData != null ? gameData.GetCurrentTarget() : currentData.minTargetTime;
    float tol = currentData.tolerance;

    bool isSuccess = (measuredTime >= (target - tol) && measuredTime <= (target + tol));

    if (isSuccess)
    {
        bool hasNext = gameData != null && gameData.NextQuestion();

        if (hasNext)
        {
            SendToOperator(StopwatchActionType.SyncResult, true);
            SetSuccess();
            StartCoroutine(NextQuestionRoutine());
        }
        else
        {
            SendToOperator(StopwatchActionType.SyncClear, true);
            SetClear();

            if (toolsOrchestrator != null)
            {
                toolsOrchestrator.ModuleSuccess();
            }
        }
    }
    else
    {
        SendToOperator(StopwatchActionType.SyncResult, false);
        SetFailed();

        if (toolsOrchestrator != null)
        {
            toolsOrchestrator.ModuleFailed();
        }

        StartCoroutine(RetryRoutine());
    }
}

    private IEnumerator NextQuestionRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        ResetTimer();
        ShowCurrentQuestion();
        SendToOperator(StopwatchActionType.ResetNext, false);
    }

    private IEnumerator RetryRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        ResetTimer();
        ShowCurrentQuestion(isRetry: true);
        SendToOperator(StopwatchActionType.ResetNext, false);
    }

    private void SendToOperator(StopwatchActionType actionType, bool isSuccess)
    {
        if (toolsOrchestrator != null)
        {
            StopwatchModuleActionPacket packet = new StopwatchModuleActionPacket
            {
                ActionType = actionType,
                IsSuccess = isSuccess
            };
            toolsOrchestrator.SendModuleInfoPacket(toolsOrchestrator.ThisModuleSettingData.ModuleVersion, packet);
        }
    }

    private void ShowInstruction(string message)
    {
        if (instructionText != null) instructionText.text = message;
    }

    public void ShowQuestion(float target, float tolerance, bool isRetry = false)
    {
        float min = target - tolerance;
        float max = target + tolerance;
        ShowInstruction($"{min:F2} - {max:F2}");
        SetNormal();
    }

    public void ReceiveStart()
    {
        if (timerManager == null) return;
        timerManager.ResetTimer();
        timerManager.StartTimer();
    }

    public void ReceiveStop()
    {
        if (timerManager == null) return;
        timerManager.StopTimer();
    }

    public void SetSuccess()
    {
        ShowInstruction("SUCCESS");
        if (timerInstructionBoard != null) timerInstructionBoard.SetSuccess();
    }

    public void SetFailed()
    {
        ShowInstruction("FAILED");
        if (timerInstructionBoard != null) timerInstructionBoard.SetFailed();
    }

    public void SetClear()
    {
        ShowInstruction("CLEAR");
        if (timerInstructionBoard != null) timerInstructionBoard.SetClear();
    }

    public void SetNormal()
    {
        if (timerInstructionBoard != null) timerInstructionBoard.SetNormal();
    }

    public float GetMeasuredTime()
    {
        return timerManager != null ? timerManager.GetCurrentTime() : 0f;
    }

    public void ResetTimer()
    {
        if (timerManager != null) timerManager.ResetTimer();
    }

    private void OnDestroy()
    {
        if (toolsOrchestrator != null && !string.IsNullOrEmpty(listenerUuid))
        {
            toolsOrchestrator.ModuleDataUnregisterListener(listenerUuid);
        }
    }
}