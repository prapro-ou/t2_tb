public class StopwatchModuleToolsOrchestrator
    : ModuleToolsOrchestratorIndividual<StopwatchModuleSettingData>
{
    /// <summary>
    /// 指示側へSTARTを送信
    /// </summary>
    public void SendStart()
    {
        SendModuleInfoPacket(
            ModuleVersionEnum.B,
            new StopwatchModuleActionPacket
            {
                Action = StopwatchActionEnum.Start
            }
        );
    }

    /// <summary>
    /// 指示側へSTOPを送信
    /// </summary>
    public void SendStop()
    {
        SendModuleInfoPacket(
            ModuleVersionEnum.B,
            new StopwatchModuleActionPacket
            {
                Action = StopwatchActionEnum.Stop
            }
        );
    }
}