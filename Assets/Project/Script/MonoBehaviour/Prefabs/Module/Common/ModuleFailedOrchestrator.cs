using UnityEngine;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class ModuleFailedOrchestrator : MonoBehaviour
{
    public void Initialize()
    {
        EOSP2PMethod.RegisterListener<ModuleFailedPacket>(OnFailedPacketReceived);
    }

    private void OnFailedPacketReceived(ProductUserId remoteUserId, string socketName, ModuleFailedPacket packet)
    {

    }
}