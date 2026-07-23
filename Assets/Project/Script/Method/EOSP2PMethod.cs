using System;
using System.Collections.Generic;
using System.Linq;
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
        private static P2PInterface P2P
        {
            get
            {
                var platformInterface = EOSManager.Instance?.GetEOSPlatformInterface();
                if (platformInterface == null)
                {
                    Debug.LogError("EOS Platform Interface の取得に失敗しました。EOSManagerの初期化完了後に呼び出してください。");
                    return null;
                }
                return platformInterface.GetP2PInterface();
            }
        }
        private static readonly byte[] _receiveBuffer = new byte[1024 * 64];

        private static Dictionary<SocketNameEnum, ulong> _notificationIds = new Dictionary<SocketNameEnum, ulong>();
        /// <summary>
        /// 内側をUUIDキーの辞書にすることで、同一のパケット型に対して複数のリスナーを同時登録できるようにしています。
        /// </summary>
        private static readonly Dictionary<string, Dictionary<Guid, Action<ProductUserId, string, byte[]>>> _packetCallbacksByType
            = new Dictionary<string, Dictionary<Guid, Action<ProductUserId, string, byte[]>>>();

        /// <summary>
        /// リスナーUUID → パケット型名 逆引き辞書。
        /// </summary>
        private static readonly Dictionary<Guid, string> _listenerTypeById
            = new Dictionary<Guid, string>();

        #endregion ========== クラス内共通処理 ==========



        #region ========== 待ち受け処理 ==========

        /// <summary>
        /// 待ち受け登録開始処理
        /// </summary>
        /// <param name="socketNameEnum">通信チャネル識別任意ソケット名</param>
        public static void StartListening(SocketNameEnum socketNameEnum)
        {

            // 0. 初期確認
            if (P2P == null || EOSManager.Instance.GetProductUserId() == null) // nullチェック
            {
                Debug.LogError("送信できません。");
                return;
            }
            if (_notificationIds.TryGetValue(socketNameEnum, out ulong existingId) && existingId != 0) // 二重登録防止
            {
                Debug.LogWarning($"[EOSP2PController] ソケット '{socketNameEnum}' は既に待ち受け中です（ID: {existingId}）。先にStopListeningを呼んでください。");
                return;
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
            _notificationIds[socketNameEnum] = notificationId;
        }

        /// <summary>
        /// 待ち受け登録終了処理
        /// </summary>
        public static void StopListening(SocketNameEnum socketNameEnum)
        {
            // 0. 初期確認：登録されていないソケットなら何もしない（例外を投げない）
            if (!_notificationIds.TryGetValue(socketNameEnum, out ulong notificationId) || notificationId == 0 || P2P == null)
            {
                return;
            }

            // 1.EOSのAPIを叩いて通知の登録を解除
            P2P.RemoveNotifyPeerConnectionRequest(notificationId);
            Debug.Log($"[EOSP2PController] 待ち受けを停止しました。ID: {notificationId}");
            _notificationIds.Remove(socketNameEnum);
        }

        /// <summary>
        /// 全待ち受け登録終了処理
        /// </summary>
        public static void StopAllListening()
        {
            foreach (var notificationId in _notificationIds.Values)
            {
                P2P.RemoveNotifyPeerConnectionRequest(notificationId);
                Debug.Log($"[EOSP2PController] 待ち受けを停止しました。ID: {notificationId}");
            }
            _notificationIds.Clear();
        }


        /// <summary>
        /// 他ユーザー接続リクエストコールバック
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



        #region ========== 送信・受信処理 ==========

        /// <summary>
        /// 送信処理
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
        /// 受信処理登録
        /// </summary>
        /// <typeparam name="T">パケット型</typeparam>
        /// <param name="onPacketReceived">パケット受信時に呼ばれるコールバック</param>
        /// <returns>登録したリスナーの識別UUID</returns>
        public static string RegisterListener<T>(Action<ProductUserId, string, T> onPacketReceived) where T : IPacketType
        {
            // 0. 初期確認
            string key = typeof(T).FullName;
            if (System.Text.Encoding.UTF8.GetByteCount(key) > 255)
            {
                Debug.LogError($"[P2P] クラス名 '{key}' が255バイトを超えています。名前を短くしてください。");
                return null;
            }
            if (onPacketReceived == null)
            {
                Debug.LogError("[P2P] コールバックがnullのため登録できません。");
                return null;
            }

            // 1. このリスナーを一意に識別するUUIDを発行
            Guid listenerId = Guid.NewGuid();

            // 2. バイト配列から型「T」へと復元し、コールバックを呼ぶラッパーを作成
            Action<ProductUserId, string, byte[]> wrappedCallback = (remoteUser, socketName, payload) =>
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

            // 3. 「型名 → (UUID → コールバック)」の辞書に登録
            if (!_packetCallbacksByType.TryGetValue(key, out var listenersForType))
            {
                listenersForType = new Dictionary<Guid, Action<ProductUserId, string, byte[]>>();
                _packetCallbacksByType[key] = listenersForType;
            }
            listenersForType[listenerId] = wrappedCallback;

            // 4. 「UUID → 型名」の逆引きも記録（解除時に型を意識せずアクセスできるようにするため）
            _listenerTypeById[listenerId] = key;

            Debug.Log($"[P2P] パケット型 '{key}' にリスナーを登録しました。ID: {listenerId}");

            return listenerId.ToString();
        }

        /// <summary>
        /// 受信登録解除(UUID)
        /// </summary>
        /// <param name="listenerId">UUID文字列</param>
        public static void UnregisterListener(string listenerId)
        {
            if (string.IsNullOrEmpty(listenerId))
            {
                Debug.LogError("[P2P] リスナーIDが空です。解除できません。");
                return;
            }
            if (!Guid.TryParse(listenerId, out Guid id))
            {
                Debug.LogError($"[P2P] リスナーID '{listenerId}' はUUID形式ではありません。");
                return;
            }
            if (!_listenerTypeById.TryGetValue(id, out string key))
            {
                Debug.LogWarning($"[P2P] リスナーID '{listenerId}' は登録されていません（既に解除済みの可能性があります）。");
                return;
            }

            if (_packetCallbacksByType.TryGetValue(key, out var listenersForType))
            {
                listenersForType.Remove(id);
                // その型のリスナーが0件になったら、型名のエントリごと削除して辞書を綺麗に保つ
                if (listenersForType.Count == 0)
                {
                    _packetCallbacksByType.Remove(key);
                }
            }
            _listenerTypeById.Remove(id);

            Debug.Log($"[P2P] パケット型 '{key}' のリスナー（ID: {listenerId}）を解除しました。");
        }

        /// <summary>
        /// 受信登録解除(パケット型)
        /// </summary>
        public static void UnregisterAllListeners<T>() where T : IPacketType
        {
            string key = typeof(T).FullName;
            if (_packetCallbacksByType.TryGetValue(key, out var listenersForType))
            {
                foreach (var id in listenersForType.Keys)
                {
                    _listenerTypeById.Remove(id);
                }
                _packetCallbacksByType.Remove(key);
                Debug.Log($"[P2P] パケット型 '{key}' の全リスナーを解除しました。");
            }
        }

        /// <summary>
        /// 受信登録全解除
        /// </summary>
        public static void UnregisterAllListeners()
        {
            _packetCallbacksByType.Clear();
            _listenerTypeById.Clear();
            Debug.Log("[P2P] 全リスナーを解除しました。");
        }

        /// <summary>
        /// 受信ループ更新
        /// </summary>
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
                    Debug.LogError($"[P2P] 受信バッファサイズを超過しているため、このパケットを破棄します。Size: {packetSize}");
                    var discardOptions = new ReceivePacketOptions()
                    {
                        LocalUserId = EOSManager.Instance.GetProductUserId(),
                        MaxDataSizeBytes = packetSize,
                        RequestedChannel = null
                    };
                    ProductUserId discardRemoteUserId = default;
                    SocketId discardSocketId = default;
                    byte discardChannel;
                    uint discardBytesWritten;
                    byte[] discardBuffer = new byte[packetSize];
                    P2P.ReceivePacket(
                        ref discardOptions,
                        ref discardRemoteUserId,
                        ref discardSocketId,
                        out discardChannel,
                        new ArraySegment<byte>(discardBuffer),
                        out discardBytesWritten
                    );
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

                if (readResult != Result.Success)
                {
                    Debug.LogWarning($"[P2P] ReceivePacketに失敗しました。Result: {readResult}");
                    continue;
                }

                if (packetSize <= 1)
                {
                    Debug.LogWarning($"[P2P] 不正な最小サイズのパケットを受信しました。Size: {packetSize}");
                    continue;
                }

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

                    if (_packetCallbacksByType.TryGetValue(typeName, out var listenersForType) && listenersForType.Count > 0)
                    {
                        // コールバック側（RegisterListener）も byte[] を直接受けるように変更するか、
                        // ここで payloadString を直接生成して渡すように変更すると、さらに byte[] の new を削減できます。
                        byte[] payload = new byte[payloadLength]; // ※ここも理想はプールか文字列への直接変換
                        Array.Copy(_receiveBuffer, payloadOffset, payload, 0, payloadLength);

                        // 同じ型名(typeName)に対して複数のリスナーが登録されていても、型判別はここで一度だけ行い、
                        // 該当する全リスナーへ同じpayloadを配信する。
                        // 列挙中にコールバック側からUnregisterListenerが呼ばれるとコレクション変更例外になるため、
                        // ToArray()でスナップショットを取ってから呼び出す。
                        foreach (var listenerEntry in listenersForType.ToArray())
                        {
                            listenerEntry.Value.Invoke(remoteUserId, socketId.SocketName, payload);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[P2P] 未登録のパケット: {typeName}");
                    }
                }
            }
        }

        #endregion ========== 送信・受信処理 ==========

    }
}