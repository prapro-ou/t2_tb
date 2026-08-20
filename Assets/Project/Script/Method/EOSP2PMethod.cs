using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using Cysharp.Threading.Tasks;
using PlayEveryWare.EpicOnlineServices;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;
using Newtonsoft.Json;

namespace OriginalNameSpace.EOSMethod.P2P
{
    /// <summary>
    /// EOS P2P通信ユーティリティ（データロスト対策・型安全対応版）
    /// </summary>
    public static class EOSP2PMethod
    {
        /// <summary>
        /// ProductUserId は EOS SDK 提供の型でソースを修正できないため、
        /// Dictionary&lt;ProductUserId, TValue&gt; のキーをNewtonsoft.Jsonが復元できるよう、
        /// クラス初期化時に TypeConverter を実行時に紐付けておく。
        /// </summary>
        static EOSP2PMethod()
        {
            TypeDescriptor.AddAttributes(typeof(ProductUserId), new TypeConverterAttribute(typeof(ProductUserIdTypeConverter)));
        }


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

        /// <summary>
        /// EOS P2Pで1パケットあたりに安全に送れる実データサイズの目安（ヘッダ込み）。
        /// これを超えるペイロードは SendPacket / UpdateReceiveLoop 内で自動的に分割・結合される。
        /// </summary>
        private const int MaxPacketPayloadBytes = 1170;

        /// <summary>
        /// 受信中の分割パケットを組み立てるためのバッファ。
        /// </summary>
        private class ChunkAssembly
        {
            public byte[][] Chunks;
            public int ReceivedCount;
        }

        /// <summary>
        /// キー: (送信元, パケット型名, メッセージID) ごとに、全チャンクが揃うまで保持する。
        /// </summary>
        private static readonly Dictionary<(ProductUserId RemoteUserId, string TypeName, Guid MessageId), ChunkAssembly> _pendingChunks
            = new Dictionary<(ProductUserId, string, Guid), ChunkAssembly>();

        /// <summary>
        /// 送受信で共通利用するJson.NETの設定。
        /// ・ProductUserId は ProductUserIdConverter で文字列として変換する
        /// ・SelfReferenceSafeContractResolver により、Color.linear や Vector3.normalized のような
        ///   「setterを持たない計算プロパティ」を型を問わず一括で除外し、自己参照ループを防ぐ
        ///   （個別の型ごとにConverterを書く必要が無くなる）
        /// ・IModuleSettingData（DialModuleSettingData等）のような、
        ///   インターフェース経由で保持されるフィールドを正しい具象型で復元できるよう
        ///   TypeNameHandling.Auto で型情報($type)をJSONに埋め込む
        /// ・TypeNameAssemblyFormatHandling.Simple により、$type文字列からバージョン/カルチャ/
        ///   公開鍵トークンを省略し、P2Pパケットサイズの肥大化(EOS_LimitExceeded)を抑える
        /// </summary>
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Converters = { new ProductUserIdConverter() },
            ContractResolver = new SelfReferenceSafeContractResolver(),
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        private static Dictionary<SocketNameEnum, ulong> _notificationIds = new Dictionary<SocketNameEnum, ulong>();

        /// <summary>
        /// 接続確立通知(AddNotifyPeerConnectionEstablished)のソケット毎の登録ID。
        /// </summary>
        private static readonly Dictionary<SocketNameEnum, ulong> _establishedNotificationIds = new Dictionary<SocketNameEnum, ulong>();

        /// <summary>
        /// 切断通知(AddNotifyPeerConnectionClosed)のソケット毎の登録ID。
        /// </summary>
        private static readonly Dictionary<SocketNameEnum, ulong> _closedNotificationIds = new Dictionary<SocketNameEnum, ulong>();

        /// <summary>
        /// 現在「接続確立済み(Established)」になっているリモートユーザーの集合。
        /// SendPacketの直前にこの集合を見て、確立済みかどうかを判定する。
        /// キー: (ソケット名, 相手のProductUserId)
        /// </summary>
        private static readonly HashSet<(SocketNameEnum SocketName, ProductUserId RemoteUserId)> _establishedPeers
            = new HashSet<(SocketNameEnum, ProductUserId)>();
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
            // Debug.Log($"[EOSP2PController] ソケット '{socketName}' での待ち受けを開始しました。ID: {notificationId}");
            _notificationIds[socketNameEnum] = notificationId;

            // 2. 接続確立通知の登録
            // 「AcceptConnectionが成功した」だけでは実通信はまだできない（NAT越え等のネゴシエーションが必要）。
            // 実際に送受信可能になったタイミングを正しく知るためにこの通知を使う。
            var establishedOptions = new AddNotifyPeerConnectionEstablishedOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                SocketId = new SocketId() { SocketName = socketName }
            };
            ulong establishedId = P2P.AddNotifyPeerConnectionEstablished(ref establishedOptions, socketNameEnum, OnPeerConnectionEstablished);
            _establishedNotificationIds[socketNameEnum] = establishedId;

            // 3. 切断通知の登録
            var closedOptions = new AddNotifyPeerConnectionClosedOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                SocketId = new SocketId() { SocketName = socketName }
            };
            ulong closedId = P2P.AddNotifyPeerConnectionClosed(ref closedOptions, socketNameEnum, OnPeerConnectionClosed);
            _closedNotificationIds[socketNameEnum] = closedId;
        }

        /// <summary>
        /// 接続確立コールバック。この通知が来て初めて、実際にパケットが送受信可能な状態になる。
        /// </summary>
        private static void OnPeerConnectionEstablished(ref OnPeerConnectionEstablishedInfo data)
        {
            SocketNameEnum socketNameEnum = (SocketNameEnum)data.ClientData;
            _establishedPeers.Add((socketNameEnum, data.RemoteUserId));
            // Debug.Log($"[P2P] 接続確立しました。Remote: {data.RemoteUserId}, Socket: {data.SocketId?.SocketName}, ConnectionType: {data.ConnectionType}");
        }

        /// <summary>
        /// 切断コールバック。確立済み集合から取り除き、以後のSendPacketでは再接続待ちとして扱われるようにする。
        /// </summary>
        private static void OnPeerConnectionClosed(ref OnRemoteConnectionClosedInfo data)
        {
            SocketNameEnum socketNameEnum = (SocketNameEnum)data.ClientData;
            _establishedPeers.Remove((socketNameEnum, data.RemoteUserId));
            Debug.LogWarning($"[P2P] 接続が切断されました。Remote: {data.RemoteUserId}, Socket: {data.SocketId?.SocketName}, Reason: {data.Reason}");
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
            // Debug.Log($"[EOSP2PController] 待ち受けを停止しました。ID: {notificationId}");
            _notificationIds.Remove(socketNameEnum);

            if (_establishedNotificationIds.TryGetValue(socketNameEnum, out ulong establishedId))
            {
                P2P.RemoveNotifyPeerConnectionEstablished(establishedId);
                _establishedNotificationIds.Remove(socketNameEnum);
            }
            if (_closedNotificationIds.TryGetValue(socketNameEnum, out ulong closedId))
            {
                P2P.RemoveNotifyPeerConnectionClosed(closedId);
                _closedNotificationIds.Remove(socketNameEnum);
            }
            // このソケットに関する確立済み状態もクリアしておく
            _establishedPeers.RemoveWhere(entry => entry.SocketName == socketNameEnum);
        }

        /// <summary>
        /// 全待ち受け登録終了処理
        /// </summary>
        public static void StopAllListening()
        {
            foreach (var notificationId in _notificationIds.Values)
            {
                P2P.RemoveNotifyPeerConnectionRequest(notificationId);
                // Debug.Log($"[EOSP2PController] 待ち受けを停止しました。ID: {notificationId}");
            }
            _notificationIds.Clear();

            foreach (var establishedId in _establishedNotificationIds.Values)
            {
                P2P.RemoveNotifyPeerConnectionEstablished(establishedId);
            }
            _establishedNotificationIds.Clear();

            foreach (var closedId in _closedNotificationIds.Values)
            {
                P2P.RemoveNotifyPeerConnectionClosed(closedId);
            }
            _closedNotificationIds.Clear();

            _establishedPeers.Clear();
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
                // Debug.Log($"[EOSP2PController] 接続を承認しました: {data.RemoteUserId} (Socket: {data.SocketId?.SocketName})");
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
            // Debug.Log($"[EOSP2PController] 送信開始: {remoteUserId} (Socket: {socketNameEnum}){packet}");
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
            string json;
            try
            {
                json = JsonConvert.SerializeObject(packet, _jsonSettings); // Newtonsoft.Jsonでシリアライズ（Dictionary/ProductUserId対応）
            }
            catch (Exception e)
            {
                // ここで例外が起きると呼び出し元のforeachが止まり、以降のユーザー全員に送信できなくなるため
                // 必ず捕捉してログに残し、該当ユーザーだけスキップする
                Debug.LogError($"[P2P] パケット型 '{className}' のシリアライズに失敗しました（送信先: {remoteUserId}）: {e}");
                return;
            }
            // Debug.Log($"[P2P] 送信データサイズ(JSON文字数): {json.Length}");
            byte[] rawData = System.Text.Encoding.UTF8.GetBytes(json); // Json文字列をバイト配列に変換

            // 1.3. チャンク分割準備
            // ヘッダ構成: [型名長 1byte][型名 classNameBytes.Length][チャンク番号 2byte][総チャンク数 2byte][メッセージID 16byte][チャンクデータ]
            // EOS P2Pは1パケットあたりの実データ量に上限があり(目安1170byte程度)、超えるとSendPacketがResult.LimitExceededで失敗する。
            // そのため、大きいペイロードはここで分割して複数パケットとして送信し、受信側で結合する。
            const int chunkIndexBytes = 2;
            const int totalChunksBytes = 2;
            const int messageIdBytes = 16;
            int headerSize = 1 + classNameBytes.Length + chunkIndexBytes + totalChunksBytes + messageIdBytes;
            int maxChunkPayloadSize = MaxPacketPayloadBytes - headerSize;
            if (maxChunkPayloadSize <= 0)
            {
                Debug.LogError($"[P2P] 型名が長すぎるため、パケットを分割できません: {className}");
                return;
            }

            int totalChunks = Math.Max(1, (int)Math.Ceiling(rawData.Length / (double)maxChunkPayloadSize));
            if (totalChunks > ushort.MaxValue)
            {
                Debug.LogError($"[P2P] パケットが大きすぎて分割数の上限を超えました。Size: {rawData.Length}bytes, 分割数: {totalChunks}");
                return;
            }
            Guid messageId = Guid.NewGuid(); // 同じ相手に対して複数のパケットを並行送信しても混線しないようにするための識別子

            // 2. 送信
            var socketId = new SocketId() { SocketName = socketName }; // ソケット識別ID

            for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                int offset = chunkIndex * maxChunkPayloadSize;
                int chunkLength = Math.Min(maxChunkPayloadSize, rawData.Length - offset);

                byte[] sendBuffer = new byte[headerSize + chunkLength];
                int writeOffset = 0;
                sendBuffer[writeOffset] = classNameLength; writeOffset += 1; // 型名長書き込み
                Array.Copy(classNameBytes, 0, sendBuffer, writeOffset, classNameBytes.Length); writeOffset += classNameBytes.Length; // 型名書き込み
                Array.Copy(BitConverter.GetBytes((ushort)chunkIndex), 0, sendBuffer, writeOffset, chunkIndexBytes); writeOffset += chunkIndexBytes; // チャンク番号書き込み
                Array.Copy(BitConverter.GetBytes((ushort)totalChunks), 0, sendBuffer, writeOffset, totalChunksBytes); writeOffset += totalChunksBytes; // 総チャンク数書き込み
                Array.Copy(messageId.ToByteArray(), 0, sendBuffer, writeOffset, messageIdBytes); writeOffset += messageIdBytes; // メッセージID書き込み
                Array.Copy(rawData, offset, sendBuffer, writeOffset, chunkLength); writeOffset += chunkLength; // Json文字列(の一部)書き込み

                var sendOptions = new SendPacketOptions() // 送信オプション
                {
                    LocalUserId = EOSManager.Instance.GetProductUserId(),
                    RemoteUserId = remoteUserId,
                    SocketId = socketId,
                    Channel = 0,
                    Reliability = reliability,
                    Data = new ArraySegment<byte>(sendBuffer)
                };
                Result sendResult = P2P.SendPacket(ref sendOptions); // EOS経由でパケットを送信
                if (sendResult != Result.Success)
                {
                    Debug.LogError($"[P2P] SendPacketに失敗しました。Result: {sendResult} (送信先: {remoteUserId}, チャンク: {chunkIndex + 1}/{totalChunks}, サイズ: {sendBuffer.Length}bytes)");
                    return; // 一部のチャンクだけ届いても受信側で復元できないため、失敗した時点で中断する
                }
            }
            // Debug.Log($"[P2P] 送信完了: {remoteUserId} (総サイズ: {rawData.Length}bytes, 分割数: {totalChunks})");
        }

        /// <summary>
        /// 指定した相手との接続が「確立済み(Established)」かどうかを返す。
        /// Acceptされただけの状態(NAT越え等のネゴシエーション中)ではtrueにならない点に注意。
        /// </summary>
        public static bool IsConnectionEstablished(SocketNameEnum socketNameEnum, ProductUserId remoteUserId)
        {
            return _establishedPeers.Contains((socketNameEnum, remoteUserId));
        }

        /// <summary>
        /// 指定した相手とのP2P接続を明示的に閉じる。
        /// ロビー退出等、相手ともう通信する必要が無くなったタイミングで呼ぶ。
        /// </summary>
        public static void CloseConnection(SocketNameEnum socketNameEnum, ProductUserId remoteUserId)
        {
            if (P2P == null || EOSManager.Instance.GetProductUserId() == null)
            {
                return;
            }
            var closeOptions = new CloseConnectionOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = remoteUserId,
                SocketId = new SocketId() { SocketName = socketNameEnum.ToString() }
            };
            Result result = P2P.CloseConnection(ref closeOptions);
            if (result != Result.Success && result != Result.NotFound)
            {
                Debug.LogWarning($"[P2P] CloseConnectionに失敗しました。Result: {result} (相手: {remoteUserId})");
            }
            _establishedPeers.Remove((socketNameEnum, remoteUserId));
        }

        /// <summary>
        /// 指定した相手との接続確立を待つ。まだ接続要求すら出していない相手の場合は、
        /// 空のパケットを1つ送ることで接続要求のトリガーとする（EOS P2Pは送信呼び出しで暗黙的に接続を開始する仕様のため）。
        /// 既に確立済みなら即座にtrueを返す。
        /// </summary>
        /// <param name="socketNameEnum">通信チャネル</param>
        /// <param name="remoteUserId">接続したい相手</param>
        /// <param name="timeoutSeconds">確立を待つ最大秒数。NAT越えに失敗した場合の無限待ちを防ぐため必須</param>
        /// <param name="cancellationToken">シーン遷移などでの中断用</param>
        /// <returns>確立に成功したかどうか</returns>
        public static async UniTask<bool> ConnectAsync(SocketNameEnum socketNameEnum, ProductUserId remoteUserId, float timeoutSeconds = 15f, CancellationToken cancellationToken = default)
        {
            if (P2P == null || EOSManager.Instance.GetProductUserId() == null)
            {
                Debug.LogError("[P2P] 接続できません。EOSManagerの初期化を確認してください。");
                return false;
            }
            if (IsConnectionEstablished(socketNameEnum, remoteUserId))
            {
                return true;
            }

            // 接続要求のトリガーとして、空の捨てパケットを送る。
            // ※中身は不要だが、UpdateReceiveLoop側のヘッダー解析で「不正なパケット」扱いされないよう、
            //   型名長=0のみの最小限の正規ヘッダー形式に合わせる（受信側では「未登録のパケット」警告で無害に処理される）。
            const int chunkIndexBytes = 2;
            const int totalChunksBytes = 2;
            const int messageIdBytes = 16;
            byte[] triggerBuffer = new byte[1 + chunkIndexBytes + totalChunksBytes + messageIdBytes];
            triggerBuffer[0] = 0; // 型名長 = 0
            Array.Copy(BitConverter.GetBytes((ushort)0), 0, triggerBuffer, 1, chunkIndexBytes); // チャンク番号 = 0
            Array.Copy(BitConverter.GetBytes((ushort)1), 0, triggerBuffer, 1 + chunkIndexBytes, totalChunksBytes); // 総チャンク数 = 1
            Array.Copy(Guid.NewGuid().ToByteArray(), 0, triggerBuffer, 1 + chunkIndexBytes + totalChunksBytes, messageIdBytes);

            var socketId = new SocketId() { SocketName = socketNameEnum.ToString() };
            var sendOptions = new SendPacketOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = remoteUserId,
                SocketId = socketId,
                Channel = 0,
                Reliability = PacketReliability.ReliableOrdered,
                Data = new ArraySegment<byte>(triggerBuffer)
            };
            Result triggerResult = P2P.SendPacket(ref sendOptions);
            if (triggerResult != Result.Success)
            {
                Debug.LogError($"[P2P] 接続要求の送信に失敗しました。Result: {triggerResult} (相手: {remoteUserId})");
                return false;
            }

            float elapsed = 0f;
            const float pollIntervalSeconds = 0.1f;
            while (!IsConnectionEstablished(socketNameEnum, remoteUserId))
            {
                if (elapsed >= timeoutSeconds)
                {
                    Debug.LogError($"[P2P] 接続確立がタイムアウトしました。相手: {remoteUserId} ({timeoutSeconds}秒)");
                    return false;
                }
                await UniTask.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken: cancellationToken);
                elapsed += pollIntervalSeconds;
            }
            return true;
        }

        /// <summary>
        /// SendPacketの安全版。接続が未確立なら確立を待ってから送信する。
        /// HostStartGame等、確実に相手に届けたい重要なパケットはこちらを使う。
        /// </summary>
        /// <returns>送信できたかどうか（タイムアウト等で失敗した場合はfalse）</returns>
        public static async UniTask<bool> SendPacketSafeAsync<T>(SocketNameEnum socketNameEnum, ProductUserId remoteUserId, T packet, float timeoutSeconds = 15f, PacketReliability reliability = PacketReliability.ReliableOrdered, CancellationToken cancellationToken = default)
            where T : IPacketType
        {
            bool connected = await ConnectAsync(socketNameEnum, remoteUserId, timeoutSeconds, cancellationToken);
            if (!connected)
            {
                return false;
            }
            SendPacket(socketNameEnum, remoteUserId, packet, reliability);
            return true;
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
                    T packetData;
                    try
                    {
                        // JSONから目的のクラス（T）へ自動復元（Newtonsoft.Json）
                        packetData = JsonConvert.DeserializeObject<T>(jsonStr, _jsonSettings);
                    }
                    catch (Exception deserializeEx)
                    {
                        // デシリアライズ自体で起きた例外。受信データやJson.NET設定側の問題である可能性が高い。
                        Debug.LogError($"[P2P] パケット型 '{key}' のデシリアライズに失敗しました（受信バイト数: {payload.Length}, JSON文字数: {jsonStr.Length}）: {deserializeEx}\nJSON内容: {jsonStr}");
                        return;
                    }

                    // 復元された綺麗なデータ（T）を添えて、登録されたコールバック（イベント）を呼び出す
                    onPacketReceived?.Invoke(remoteUser, socketName, packetData);
                }
                catch (Exception e)
                {
                    // デシリアライズは成功したが、その後の onPacketReceived（呼び出し元の実装）側で起きた例外
                    Debug.LogError($"[P2P] パケット型 '{key}' の受信コールバック処理中にエラーが発生しました: {e}");
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

            // Debug.Log($"[P2P] パケット型 '{key}' にリスナーを登録しました。ID: {listenerId}");

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

            // Debug.Log($"[P2P] パケット型 '{key}' のリスナー（ID: {listenerId}）を解除しました。");
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
                // Debug.Log($"[P2P] パケット型 '{key}' の全リスナーを解除しました。");
            }
        }

        /// <summary>
        /// 受信登録全解除
        /// </summary>
        public static void UnregisterAllListeners()
        {
            _packetCallbacksByType.Clear();
            _listenerTypeById.Clear();
            // Debug.Log("[P2P] 全リスナーを解除しました。");
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
                Debug.Log("受け取りました。");
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
                // Debug.Log($"[P2P] ReceivePacket成功: Size={bytesWritten}bytes, From={remoteUserId}");
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
                    const int chunkIndexBytes = 2;
                    const int totalChunksBytes = 2;
                    const int messageIdBytes = 16;
                    int headerSizeWithoutPayload = 1 + typeNameLength + chunkIndexBytes + totalChunksBytes + messageIdBytes;
                    if (packetSize < headerSizeWithoutPayload)
                    {
                        Debug.LogError("[P2P] パケットサイズが不正です。");
                        continue;
                    }

                    // 文字列化は避けられないが、元の配列から直接デコードする
                    string typeName = System.Text.Encoding.UTF8.GetString(_receiveBuffer, 1, typeNameLength);
                    int readOffset = 1 + typeNameLength;
                    ushort chunkIndex = BitConverter.ToUInt16(_receiveBuffer, readOffset); readOffset += chunkIndexBytes;
                    ushort totalChunks = BitConverter.ToUInt16(_receiveBuffer, readOffset); readOffset += totalChunksBytes;
                    byte[] messageIdBuffer = new byte[messageIdBytes];
                    Array.Copy(_receiveBuffer, readOffset, messageIdBuffer, 0, messageIdBytes);
                    Guid messageId = new Guid(messageIdBuffer);
                    readOffset += messageIdBytes;

                    int chunkPayloadLength = (int)packetSize - readOffset;
                    byte[] chunkPayload = new byte[chunkPayloadLength]; // ※ここも理想はプールか文字列への直接変換
                    Array.Copy(_receiveBuffer, readOffset, chunkPayload, 0, chunkPayloadLength);

                    byte[] payload;
                    if (totalChunks <= 1)
                    {
                        // 分割されていない通常のパケット
                        payload = chunkPayload;
                    }
                    else
                    {
                        // 送信側で分割されたパケット。全チャンクが揃うまでバッファしておく。
                        var key = (remoteUserId, typeName, messageId);
                        if (!_pendingChunks.TryGetValue(key, out var assembly))
                        {
                            assembly = new ChunkAssembly { Chunks = new byte[totalChunks][], ReceivedCount = 0 };
                            _pendingChunks[key] = assembly;
                        }
                        if (assembly.Chunks[chunkIndex] == null)
                        {
                            assembly.Chunks[chunkIndex] = chunkPayload;
                            assembly.ReceivedCount++;
                        }
                        if (assembly.ReceivedCount < totalChunks)
                        {
                            // まだ全チャンクが揃っていないので、このパケット単体では処理せず次の受信へ進む
                            continue;
                        }

                        // 全チャンクが揃ったので結合して1つのペイロードに復元する
                        int totalLength = 0;
                        foreach (var chunk in assembly.Chunks)
                        {
                            totalLength += chunk.Length;
                        }
                        payload = new byte[totalLength];
                        int writePos = 0;
                        foreach (var chunk in assembly.Chunks)
                        {
                            Array.Copy(chunk, 0, payload, writePos, chunk.Length);
                            writePos += chunk.Length;
                        }
                        _pendingChunks.Remove(key);
                    }

                    if (_packetCallbacksByType.TryGetValue(typeName, out var listenersForType) && listenersForType.Count > 0)
                    {
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