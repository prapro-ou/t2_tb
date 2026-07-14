using System;
using System.Collections.Generic;
using PlayEveryWare.EpicOnlineServices;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;

namespace OriginalNameSpace.EOSMethod.P2P
{
    /// <summary>
    /// EOS P2P通信ユーティリティ（データロスト対策・型安全対応版）
    /// </summary>
    public static class EOSP2PMethod
    {


        #region ========== クラス内共通処理 ==========

        // 開発元（PlayEveryWare製ラッパー）のEOSManagerから、P2P通信に必要な低レイヤーインターフェースを毎回安全に取得します。
        private static P2PInterface P2P => EOSManager.Instance.GetEOSPlatformInterface()?.GetP2PInterface();
        private static readonly byte[] _receiveBuffer = new byte[1024 * 64];
        /// <summary>
        /// 【重要】受信したパケットの「タイプ（1バイト目）」と「それを処理するデシリアライズ処理」を紐付ける辞書。
        /// 1つの受信ループで全てのパケットを回収し、この辞書を頼りに適切な型へと安全に分配（ディスパッチ）します。
        /// </summary>
        private static readonly Dictionary<string, Action<ProductUserId, string, byte[]>> _packetCallbacks
            = new Dictionary<string, Action<ProductUserId, string, byte[]>>();

        #endregion ========== クラス内共通処理 ==========



        #region ========== 待ち受け処理 ==========

        /// <summary>
        /// 待ち受け登録開始
        /// </summary>
        /// <param name="socketName">通信チャネル識別任意ソケット名</param>
        /// <returns>待ち受け解除時に必要な通知ID</returns>
        public static ulong StartListening(SocketNameEnum socketNameEnum)
        {

            // 0. 初期確認
            if (P2P == null || EOSManager.Instance.GetProductUserId() == null) // nullチェック
            {
                Debug.LogError("送信できません。");
                return 0;
            }
            string socketName = socketNameEnum.ToString(); // ソケット名のstring化

            // 1. EOSAPIを叩き待ち受け開始
            var options = new AddNotifyPeerConnectionRequestOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(), // ログイン中の一意なユーザーIDを指定
                SocketId = new SocketId() { SocketName = socketName }
            };
            ulong notificationId = P2P.AddNotifyPeerConnectionRequest(ref options, null, OnIncomingConnectionRequest);
            Debug.Log($"[EOSP2PController] ソケット '{socketName}' での待ち受けを開始しました。ID: {notificationId}");

            // 2. 通知ID返却
            return notificationId;
        }

        /// <summary>
        /// 待ち受け登録終了
        /// </summary>
        public static void StopListening(ulong notificationId)
        {

            // 0. 初期確認
            if (notificationId == 0 || P2P == null) return;

            // 1.EOSのAPIを叩いて通知の登録を解除
            P2P.RemoveNotifyPeerConnectionRequest(notificationId);
            Debug.Log($"[EOSP2PController] 待ち受けを停止しました。ID: {notificationId}");

        }

        /// <summary>
        /// 他のユーザーから接続リクエスト（握手要求）が届いた際に呼ばれるEOSコールバック。
        /// ※この実装では届いた要求をすべて自動的に承認（Accept）します。
        /// </summary>
        private static void OnIncomingConnectionRequest(ref OnIncomingConnectionRequestInfo data)
        {
            // 0. 初期確認
            if (P2P == null)
            {
                Debug.LogError("送信できません。");
                return;
            }

            // 1. 接続承認
            var acceptOptions = new AcceptConnectionOptions() // 届いたリクエスト情報を承認オプションに設定
            {
                LocalUserId = data.LocalUserId,
                RemoteUserId = data.RemoteUserId,
                SocketId = data.SocketId
            };
            Result result = P2P.AcceptConnection(ref acceptOptions); // 接続承認実行。承認されればパケットの送受信が可能に
            if (result == Result.Success)
            {
                Debug.Log($"[EOSP2PController] 接続を承認しました: {data.RemoteUserId} (Socket: {data.SocketId?.SocketName})");
            }
        }

        #endregion ========== 待ち受け処理 ==========



        #region ================= 送信・受信（データロスト防止・分配型） =================

        /// <summary>
        /// 【送信】任意の構造体/クラスデータをJsonUtilityでシリアライズし、先頭にヘッダー（型識別子）を付与して送信します。
        /// </summary>
        /// <typeparam name="T">IPacketTypeを実装するデータ型</typeparam>
        /// <param name="socketName">通信チャネル識別任意ソケット名</param>
        /// <param name="remoteUserId">送信相手のEOSユーザーID（ProductUserId）</param>
        /// <param name="packet">送信したいデータ</param>
        /// <param name="reliability">送信方法</param>
        public static void SendPacket<T>(SocketNameEnum socketNameEnum, ProductUserId remoteUserId, T packet, PacketReliability reliability = PacketReliability.ReliableOrdered)
            where T : IPacketType
        {

            // 0. 初期確認
            if (P2P == null || EOSManager.Instance.GetProductUserId() == null) // nullチェック
            {
                Debug.LogError("送信できません。");
                return;
            }
            string socketName = socketNameEnum.ToString(); // ソケット名のstring化

            // 1. 送信データ制作
            // 1.1. データ型名をバイト配列に変換
            string className = typeof(T).FullName; // 型名文字列取得
            byte[] classNameBytes = System.Text.Encoding.UTF8.GetBytes(className); // 型名文字列をバイト配列に変換
            if (classNameBytes.Length > 255) // 型名の長さチェック
            {
                Debug.LogError($"[P2P] クラス名が長すぎます: {className}");
                return;
            }
            byte classNameLength = (byte)classNameBytes.Length; // 型名の長さを1バイトに変換
            // 1.2. データ内部をバイト配列に変換
            string json = JsonUtility.ToJson(packet); // JsonUtilityでシリアライズ
            byte[] rawData = System.Text.Encoding.UTF8.GetBytes(json); // Json文字列をバイト配列に変換
            // 1.3. 全体のバッファを確保
            byte[] sendBuffer = new byte[1 + classNameBytes.Length + rawData.Length]; // 型名長+型名+Json文字列
            // 1.4. バッファへの書き込み
            sendBuffer[0] = classNameLength; // 型名長書き込み
            Array.Copy(classNameBytes, 0, sendBuffer, 1, classNameBytes.Length); // 型名書き込み
            Array.Copy(rawData, 0, sendBuffer, 1 + classNameBytes.Length, rawData.Length); // Json文字列書き込み

            // 2. 送信
            var socketId = new SocketId() { SocketName = socketName }; // ソケット識別ID
            var sendOptions = new SendPacketOptions() // 送信オプション
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = remoteUserId,
                SocketId = socketId,
                Channel = 0,
                Reliability = reliability,
                Data = new ArraySegment<byte>(sendBuffer)
            };
            P2P.SendPacket(ref sendOptions); // EOS経由でパケットを送信
        }

        /// <summary>
        /// 型指定を伴う受信処理登録
        /// </summary>
        /// <typeparam name="T">パケット型</typeparam>
        /// <param name="onPacketReceived">パケット受信時に呼ばれるコールバック</param>
        public static void RegisterListener<T>(Action<ProductUserId, string, T> onPacketReceived) where T : IPacketType
        {
            // 0. 初期確認
            string key = typeof(T).FullName;
            if (System.Text.Encoding.UTF8.GetByteCount(key) > 255)
            {
                Debug.LogError($"[P2P] クラス名 '{key}' が255バイトを超えています。名前を短くしてください。");
                return;
            }

            // 1. バイト配列から型「T」へと復元し、コールバックを呼ぶ
            _packetCallbacks[key] = (remoteUser, socketName, payload) =>
            {
                try
                {
                    // バイト配列をJSON文字列に戻す
                    string jsonStr = System.Text.Encoding.UTF8.GetString(payload);

                    // JSONから目的のクラス（T）へ自動復元
                    T packetData = JsonUtility.FromJson<T>(jsonStr);

                    // 復元された綺麗なデータ（T）を添えて、登録されたコールバック（イベント）を呼び出す
                    onPacketReceived?.Invoke(remoteUser, socketName, packetData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[P2P] パケット型 '{key}' のデシリアライズに失敗しました: {e.Message}");
                }
            };
        }

        /// <summary>
        /// 指定したパケットタイプに対する受信登録を解除します。シーン遷移時やオブジェクト破棄時に呼んでください。
        /// </summary>
        public static void UnregisterListener<T>() where T : IPacketType
        {
            string key = typeof(T).FullName;
            if (System.Text.Encoding.UTF8.GetByteCount(key) > 255)
            {
                Debug.LogError($"[P2P] クラス名 '{key}' が255バイトを超えています。名前を短くしてください。");
                return;
            }

            if (_packetCallbacks.ContainsKey(key))
            {
                _packetCallbacks.Remove(key);
                Debug.Log($"[P2P] パケット型 '{key}' のリスナーを解除しました。");
            }
        }

        public static void UpdateReceiveLoop()
        {
            // ログ出力を削除し、安全に早期リターン
            if (P2P == null || !EOSManager.Instance.HasLoggedInWithConnect() || EOSManager.Instance.GetProductUserId() == null)
            {
                return;
            }

            var getNextSizeOptions = new GetNextReceivedPacketSizeOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RequestedChannel = null
            };

            while (P2P.GetNextReceivedPacketSize(ref getNextSizeOptions, out uint packetSize) == Result.Success && packetSize > 0)
            {
                // 確保した固定バッファより大きいパケットはエラー
                if (packetSize > _receiveBuffer.Length)
                {
                    Debug.LogError($"[P2P] 受信バッファサイズを超過しています。Size: {packetSize}");
                    // パケットを破棄するために一度ダミーで読み飛ばすなどの処理が必要な場合があります
                    continue;
                }

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

                // 既存のバッファ配列を再利用（new しない）
                Result readResult = P2P.ReceivePacket(
                    ref receiveOptions,
                    ref remoteUserId,
                    ref socketId,
                    out channel,
                    new ArraySegment<byte>(_receiveBuffer, 0, (int)packetSize),
                    out bytesWritten
                );

                if (readResult == Result.Success && packetSize > 1)
                {
                    int typeNameLength = _receiveBuffer[0];
                    if (packetSize < 1 + typeNameLength)
                    {
                        Debug.LogError("[P2P] パケットサイズが不正です。");
                        continue;
                    }

                    // 文字列化は避けられないが、元の配列から直接デコードする
                    string typeName = System.Text.Encoding.UTF8.GetString(_receiveBuffer, 1, typeNameLength);

                    int payloadOffset = 1 + typeNameLength;
                    int payloadLength = (int)packetSize - payloadOffset;

                    if (_packetCallbacks.TryGetValue(typeName, out var callback))
                    {
                        // コールバック側（RegisterListener）も byte[] を直接受けるように変更するか、
                        // ここで payloadString を直接生成して渡すように変更すると、さらに byte[] の new を削減できます。
                        byte[] payload = new byte[payloadLength]; // ※ここも理想はプールか文字列への直接変換
                        Array.Copy(_receiveBuffer, payloadOffset, payload, 0, payloadLength);

                        callback.Invoke(remoteUserId, socketId.SocketName, payload);
                    }
                    else
                    {
                        Debug.LogWarning($"[P2P] 未登録のパケット: {typeName}");
                    }
                }
            }
        }

        #endregion
    }
}