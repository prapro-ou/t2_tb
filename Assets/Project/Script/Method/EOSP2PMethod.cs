using System;
using PlayEveryWare.EpicOnlineServices;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;

namespace OriginalNameSpace.EOSMethod.P2P
{
    /// <summary>
    /// EOSP2P静的ロジッククラス。
    /// </summary>
    public static class EOSP2PMethod
    {
        // P2P API
        private static P2PInterface P2P => EOSManager.Instance.GetEOSPlatformInterface()?.GetP2PInterface();

        /// <summary>
        /// 接続要求の待ち受けを開始し、解除に必要なIDを返す。
        /// </summary>
        /// <param name="socketName">識別ソケット名</param>
        /// <returns>NotificationID</returns>
        public static ulong StartListening(string socketName)
        {
            if (P2P == null) return 0;

            var socketId = new SocketId() { SocketName = socketName };
            var options = new AddNotifyPeerConnectionRequestOptions() { SocketId = socketId };

            // 共通プロパティ P2P を使用するように一貫性を確保
            ulong notificationId = P2P.AddNotifyPeerConnectionRequest(ref options, null, OnIncomingConnectionRequest);
            Debug.Log($"[EOSP2PController] ソケット '{socketName}' での待ち受けを開始しました。ID: {notificationId}");

            return notificationId;
        }

        /// <summary>
        /// 【停止】指定されたNotificationIDの待ち受けを停止します。
        /// </summary>
        public static void StopListening(ulong notificationId)
        {
            if (notificationId != 0 && P2P != null)
            {
                P2P.RemoveNotifyPeerConnectionRequest(notificationId);
                Debug.Log($"[EOSP2PController] 待ち受けを停止しました。ID: {notificationId}");
            }
        }

        /// <summary>
        /// 接続要求が届いた時にEOSから自動で呼ばれるコールバック（自動で承認）
        /// </summary>
        private static void OnIncomingConnectionRequest(ref OnIncomingConnectionRequestInfo data)
        {
            if (P2P == null) return;

            var acceptOptions = new AcceptConnectionOptions()
            {
                LocalUserId = data.LocalUserId,
                RemoteUserId = data.RemoteUserId,
                SocketId = data.SocketId
            };

            Result result = P2P.AcceptConnection(ref acceptOptions);
            if (result == Result.Success)
            {
                Debug.Log($"[EOSP2PController] 接続を承認しました: {data.RemoteUserId} (Socket: {data.SocketId?.SocketName})");
            }
        }

        /// <summary>
        /// 【送信】指定したソケット名で相手にデータを送信します。
        /// </summary>
        public static void SendPacketData(string socketName, ProductUserId remoteUserId, byte[] data, PacketReliability reliability = PacketReliability.ReliableOrdered)
        {
            if (P2P == null) return;

            var socketId = new SocketId() { SocketName = socketName };
            var sendOptions = new SendPacketOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = remoteUserId,
                SocketId = socketId,
                Channel = 0,
                Reliability = reliability,
                Data = new ArraySegment<byte>(data)
            };

            Result result = P2P.SendPacket(ref sendOptions);
            if (result != Result.Success)
            {
                Debug.LogError($"[EOSP2PController] 送信失敗: {result}");
            }
        }

        /// <summary>
        /// 【受信チェック】新しく届いたパケットがないか確認し、あれば引数のアクションを実行します。
        /// </summary>
        /// <param name="onDataReceivedAction">データ受信時に実行したいコールバック関数 (送信元ID, ソケット名, 受信バイト列)</param>
        public static void UpdateReceiveLoop(Action<ProductUserId, string, byte[]> onDataReceivedAction)
        {
            if (P2P == null || !EOSManager.Instance.HasLoggedInWithConnect()) return;

            var getNextSizeOptions = new GetNextReceivedPacketSizeOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RequestedChannel = null
            };

            // while ループに変更し、そのフレームに届いているパケットをすべて処理する
            while (P2P.GetNextReceivedPacketSize(ref getNextSizeOptions, out uint packetSize) == Result.Success && packetSize > 0)
            {
                byte[] buffer = new byte[packetSize];
                var receiveOptions = new ReceivePacketOptions()
                {
                    LocalUserId = EOSManager.Instance.GetProductUserId(),
                    MaxDataSizeBytes = packetSize,
                    RequestedChannel = null
                };

                ProductUserId remoteUserId = default;
                SocketId socketId = default;
                byte channel;
                uint bytesWritten;

                Result readResult = P2P.ReceivePacket(ref receiveOptions, ref remoteUserId, ref socketId, out channel, new ArraySegment<byte>(buffer), out bytesWritten);

                if (readResult == Result.Success)
                {
                    onDataReceivedAction?.Invoke(remoteUserId, socketId.SocketName, buffer);
                }
            }
        }
    }
}