using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
using OriginalNameSpace.EOSMethod.P2P;
public class GameSceneStartGameOperator : MonoBehaviour
{
    public void Initialize()
    {
        EOSP2PMethod.RegisterListener<StartGamePacket>(StartGame);
    }

    public void StartGame(ProductUserId remoteUserId, string socketName, StartGamePacket packet)
    {
        EOSP2PMethod.StartListening(SocketNameEnum.ModuleInfo);
    }
}