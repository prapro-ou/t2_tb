using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using UnityEngine;
namespace OriginalNameSpace
{
    public static class EOSLoginMehod
    {
        public static void LoginWithDeviceID(ConnectInterface connectInterface)
        {
            // 端末固有の識別名（任意）
            string deviceModel = SystemInfo.deviceModel;

            var loginOptions = new LoginOptions
            {
                Credentials = new Credentials
                {
                    Type = ExternalCredentialType.DeviceidAccessToken,
                    Token = deviceModel
                },
                UserLoginInfo = new UserLoginInfo { DisplayName = "HostPlayer" } // プレイヤー名
            };

            connectInterface.Login(ref loginOptions, null, (ref LoginCallbackInfo callbackInfo) =>
            {
                if (callbackInfo.ResultCode == Result.Success)
                {
                    Debug.Log($"Epicアカウントなしでのログイン成功！ ProductUserId: {callbackInfo.LocalUserId}");
                }
                else if (callbackInfo.ResultCode == Result.InvalidUser)
                {
                    // 初回起動時はDevice IDが存在しないため、作成処理へ移る
                    CreateDeviceID(connectInterface);
                }
                else
                {
                    Debug.LogError($"ログイン失敗: {callbackInfo.ResultCode}");
                }
            });
        }

        private static void CreateDeviceID(ConnectInterface connectInterface)
        {
            var createDeviceIdOptions = new CreateDeviceIdOptions
            {
                DeviceModel = SystemInfo.deviceModel
            };

            connectInterface.CreateDeviceId(ref createDeviceIdOptions, null, (ref CreateDeviceIdCallbackInfo callbackInfo) =>
            {
                if (callbackInfo.ResultCode == Result.Success || callbackInfo.ResultCode == Result.DuplicateNotAllowed)
                {
                    Debug.Log("Device IDの作成成功。再度ログインします。");
                    LoginWithDeviceID(connectInterface); // 作成できたらもう一度ログイン
                }
                else
                {
                    Debug.LogError($"Device ID作成失敗: {callbackInfo.ResultCode}");
                }
            });
        }
    }
}