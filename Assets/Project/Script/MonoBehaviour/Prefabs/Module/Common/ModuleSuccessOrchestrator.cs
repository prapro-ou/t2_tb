using UnityEngine;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class ModuleSuccessOrchestrator : MonoBehaviour
{
    private ProductUserId _hostUserId;
    public void Initialize(ProductUserId productUserId)
    {
        _hostUserId = productUserId;
        EOSP2PMethod.RegisterListener<ModuleSuccessPacket>(OnSuccessPacketReceived);
    }

    private void OnSuccessPacketReceived(ProductUserId remoteUserId, string socketName, ModuleSuccessPacket packet)
    {

    }
}