using Epic.OnlineServices.Connect;
using UnityEngine;
using PlayEveryWare.EpicOnlineServices;
using OriginalNameSpace;
public class EOSLogin : MonoBehaviour
{
    private ConnectInterface _connectInterface;

    void Start()
    {
        _connectInterface = EOSManager.Instance.GetEOSPlatformInterface().GetConnectInterface();

        // 1. まずはバックグラウンドでDevice IDを使ってログインを試みる
        EOSLoginMehod.LoginWithDeviceID(_connectInterface);
    }
}