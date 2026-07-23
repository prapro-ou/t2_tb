using UnityEngine;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class ModuleSuccessOrchestrator : MonoBehaviour
{

    public void Initialize()
    {
        EOSP2PMethod.RegisterListener<ModuleSuccessPacket>(OnSuccessPacketReceived);
    }

    private void OnSuccessPacketReceived(ProductUserId remoteUserId, string socketName, ModuleSuccessPacket packet)
    {

    }
}