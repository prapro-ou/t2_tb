using UnityEngine;
using TMPro;

public class OperatorGameManager : MonoBehaviour
{
    [SerializeField] private StopwatchToolsOrchestrator toolsOrchestrator;
    [SerializeField] private ActionButtonController actionButtonController;
    [SerializeField] private BoardController boardController; // 💡 BoardControllerの参照

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;

    private bool isInitialized = false;
    private bool isRunning = false;
    private bool isLocked = false;
    private string listenerUuid;

    public void OnInitialize(StopwatchModuleSettingData data)
    {
        this.isInitialized = true;
        ResetOperatorUI();

        if (toolsOrchestrator != null)
        {
            listenerUuid = toolsOrchestrator.ModuleDataRegisterListener<StopwatchModuleActionPacket>((version, packet) =>
            {
                switch (packet.ActionType)
                {
                    case StopwatchActionType.SyncResult:
                        ApplyResult(packet.IsSuccess);
                        break;
                    case StopwatchActionType.ResetNext:
                        ResetOperatorUI();
                        break;
                    case StopwatchActionType.SyncClear:
                        ApplyClear();
                        break;
                }
            });
        }
    }

    private void ApplyResult(bool isSuccess)
    {
        isLocked = true;

        if (instructionText != null) instructionText.text = isSuccess ? "SUCCESS" : "FAILED";
        if (actionButtonController != null)
        {
            if (isSuccess) actionButtonController.SetSuccess();
            else actionButtonController.SetFailed();
        }

        // 💡 背景板（InstructionBoard）の色を変更
        if (boardController != null)
        {
            if (isSuccess) boardController.SetSuccess();
            else boardController.SetFailed();
        }
    }

    private void ApplyClear()
    {
        isLocked = true;

        if (instructionText != null) instructionText.text = "MODULE CLEAR!";
        if (actionButtonController != null) actionButtonController.SetClear();

        // 💡 背景板（InstructionBoard）の色を黄色に変更
        if (boardController != null) boardController.SetClear();
    }

    public void ResetOperatorUI()
    {
        isRunning = false;
        isLocked = false;

        if (instructionText != null) instructionText.text = "PRESS BUTTON";
        if (actionButtonController != null) actionButtonController.SetPlay();

        // 💡 背景板（InstructionBoard）の色を白色（Normal）にリセット
        if (boardController != null) boardController.SetNormal();
    }

    public void PressActionButton()
    {
        if (!isInitialized || isLocked) return;

        isRunning = !isRunning;

        if (actionButtonController != null)
        {
            if (isRunning) actionButtonController.SetStop();
            else actionButtonController.SetPlay();
        }

        if (toolsOrchestrator != null)
        {
            StopwatchModuleActionPacket packet = new StopwatchModuleActionPacket
            {
                ActionType = StopwatchActionType.ToggleTimer,
                IsRunning = this.isRunning
            };

            toolsOrchestrator.SendModuleInfoPacket(toolsOrchestrator.ThisModuleSettingData.ModuleVersion, packet);
        }
    }

    private void OnDestroy()
    {
        if (toolsOrchestrator != null && !string.IsNullOrEmpty(listenerUuid))
        {
            toolsOrchestrator.ModuleDataUnregisterListener(listenerUuid);
        }
    }
}