using UnityEngine;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class LobbySceneP2PConnectOperator : MonoBehaviour
{
    public void Initialize()
    {
        EOSP2PMethod.StartListening(SocketNameEnum.Test);
        EOSP2PMethod.RegisterListener<TestPacket>(OnPacketReceived);
    }

    public void OnPacketReceived(ProductUserId remoteUserId, string socketName, TestPacket packetData)
    {
        Debug.Log(remoteUserId.ToString());
        Debug.Log(socketName);
        Debug.Log(packetData.message);
    }
    public void Update()
    {
        EOSP2PMethod.UpdateReceiveLoop();
    }
}