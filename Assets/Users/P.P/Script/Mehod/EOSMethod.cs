using PlayEveryWare.EpicOnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices;
using UnityEngine;
using Cysharp.Threading.Tasks; // UniTaskの非同期処理（async/await）を利用するために必要

namespace OrginalNamespace
{
    /// <summary>
    /// Epic Online Services (EOS) の各種手続き（認証・通信・ロビー）を管理する静的クラス
    /// </summary>
    public static class EOSMethod
    {
        /// <summary>
        /// 端末の「デバイスID」を使用して、EOS Connect サービスへのログインを非同期で実行します。
        /// 成功時はプレイヤーの識別子（ProductUserId）を返し、失敗時は null を返します。
        /// </summary>
        public static async UniTask<ProductUserId> EOSLoginAsync()
        {
            // 1. EOSのユーザー認証・接続を司る Connect インターフェースを取得
            var connectInterface = EOSManager.Instance.GetEOSConnectInterface();
            if (connectInterface == null)
            {
                Debug.LogError("Connect Interface の取得に失敗しました。");
                return null;
            }

            // 2. 端末固有デバイスID作成設定を構築
            var createDeviceIdOptions = new CreateDeviceIdOptions
            {
                DeviceModel = SystemInfo.deviceModel //端末のモデル名
            };

            // 3.EOSサーバーへのデバイスID作成リクエスト
            var deviceIdUtcs = new UniTaskCompletionSource<CreateDeviceIdCallbackInfo>(); // UniTask用待機ソース
            connectInterface.CreateDeviceId(ref createDeviceIdOptions, null, (ref CreateDeviceIdCallbackInfo createDeviceIdData) =>
            {
                deviceIdUtcs.TrySetResult(createDeviceIdData); //結果を返す
            });

            // 4.非同期待機
            CreateDeviceIdCallbackInfo deviceIdResult = await deviceIdUtcs.Task;

            // 5.デバイスIDの作成結果を確認
            if (deviceIdResult.ResultCode == Result.Success || // ID作成成功
                deviceIdResult.ResultCode == Result.DuplicateNotAllowed) // ID作成済み（実質成功）
            {
                Debug.Log("デバイスIDの準備完了。StartConnectLoginWithOptions を実行します。");

                // 5.1.ログインオプションを構築
                var loginOptions = new LoginOptions
                {
                    Credentials = new Credentials
                    {
                        Type = ExternalCredentialType.DeviceidAccessToken, // 認証タイプ指定
                        Token = null // アクセストークン(DeviceIdならnull)
                    }
                };

                // 5.2.ログイン処理を実行
                var loginUtcs = new UniTaskCompletionSource<ProductUserId>(); // UniTask用待機ソース
                EOSManager.Instance.StartConnectLoginWithOptions( // EOS Connect へのログインリクエスト
                    loginOptions,
                    (LoginCallbackInfo loginData) =>
                    {
                        // ログイン処理の結果が返ってきたときのコールバック
                        if (loginData.ResultCode == Result.Success)
                        {
                            Debug.Log($"EOS Connect ログイン成功! PUID: {loginData.LocalUserId}");
                            // ログイン成功：取得したユーザー固有の ProductUserId をセットして待機を解除
                            loginUtcs.TrySetResult(loginData.LocalUserId);
                        }
                        else
                        {
                            Debug.LogError($"Login 失敗。ステータスコード: {loginData.ResultCode}");
                            // ログイン失敗：null をセットして待機を解除
                            loginUtcs.TrySetResult(null);
                        }
                    }
                );
                // ログインが完了して ProductUserId が手に入るまで待機し、呼び出し元に返す
                return await loginUtcs.Task;
            }
            else
            {
                // デバイスIDの作成自体に失敗した場合はエラーログを出して終了
                Debug.LogError($"デバイスIDの作成・取得に失敗しました: {deviceIdResult}");
                return null;
            }
        }

        /// <summary>
        /// P2P（ピア・ツー・ピア）通信を行うための受信リスナー（待ち受け状態）を初期化します。
        /// 他のプレイヤーからの接続要求を自動的に検知して承認する役割を持ちます。
        /// </summary>
        public static void InitializeP2P()
        {
            // EOSのP2P（1対1の直接通信）を司る P2P インターフェースを取得
            var p2pInterface = EOSManager.Instance.GetEOSPlatformInterface().GetP2PInterface();

            // 通信の接続要求を監視（リッスン）するための設定を構築
            var addNotifyOptions = new AddNotifyPeerConnectionRequestOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(), // リクエストを待ち受ける自分のProductUserId
                SocketId = new SocketId() { SocketName = "GameTraffic" } // この通信で使用する任意のソケット識別名
            };

            // 他のプレイヤーから「接続したい」という要求（Request）が届いたときに走るイベント通知を登録
            var _p2pListenerId = p2pInterface.AddNotifyPeerConnectionRequest(ref addNotifyOptions, null,
            (ref OnIncomingConnectionRequestInfo data) =>
            {
                // 接続要求が届いた際のコールバック処理
                Debug.Log($"[{data.RemoteUserId}] から接続要求が来ました。承認します。");

                // 要求元の相手と通信を確立（承認）するための設定を構築
                var acceptOptions = new AcceptConnectionOptions()
                {
                    LocalUserId = EOSManager.Instance.GetProductUserId(), // 自分のID
                    RemoteUserId = data.RemoteUserId, // 接続を許可する相手のID
                    SocketId = data.SocketId // 要求が届いたソケット情報
                };

                // 相手からの接続を正式に承認。これで双方向のP2Pパケット送信（SendData）が可能になります
                p2pInterface.AcceptConnection(ref acceptOptions);
            });
        }

        /// <summary>
        /// 指定した「部屋名（合言葉）」を検索バケットに含めたプライベートロビーを非同期で作成します。
        /// 成功時は作成されたロビー固有の LobbyId を返し、失敗時は空文字（string.Empty）を返します。
        /// </summary>
        public static async UniTask<string> CreateGameLobbyAsync(string roomName)
        {
            // 事前チェック：部屋名が空っぽの場合は処理を行わない
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogError("ロビー名を入力してください。");
                return string.Empty;
            }

            // 1. ロビーの作成や検索、管理を司る Lobby インターフェースを取得
            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

            // 2. 現在ログインが完了している自分の ProductUserId を取得・検証
            ProductUserId localProductUserId = EOSManager.Instance.GetProductUserId();
            if (localProductUserId == null || !localProductUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビーを作成できません。");
                return string.Empty;
            }

            // 3. これから作成するロビーの構成パラメーター（オプション）を構築
            var createLobbyOptions = new CreateLobbyOptions()
            {
                // === 【必須】基本設定 ===
                LocalUserId = localProductUserId, // ロビーの所有者（ホスト）となる自分のID
                MaxLobbyMembers = 2, // ロビーに入れる最大人数（今回は1vs1対戦などを想定し2人に設定）
                PermissionLevel = LobbyPermissionLevel.Inviteonly, // ロビーの公開範囲設定（合言葉等によるInviteonlyを指定）

                // === 【重要】検索と招待の設定 ===
                PresenceEnabled = false, // Epic Games Launcher等のフレンド機能にこのロビー情報を同期させるか（合言葉のみにするためオフ）
                AllowInvites = true,     // ホスト以外の一般メンバーが他の人をこのロビーに招待することを許可するか
                BucketId = $"PrivateRoom_{roomName}", // ★超重要：検索時の合言葉の役割を果たすフィルター文字列
                LobbyId = null,          // 特定の文字列で部屋IDを固定したい場合以外はnull。サーバーが自動で一意のIDを発行します
                EnableJoinById = true,   // 発行されたロビーID（文字列）を直接指定した入室（Join）を許可するか

                // === 【ボイスチャット（RTC）機能】 ===
                EnableRTCRoom = false,  // ロビーに連動した音声通話の部屋（RTCルーム）を自動生成するか（今回は不使用）
                LocalRTCOptions = null, // ボイスチャット利用時のローカルデバイス詳細オプション（不使用のためnull）
                RTCRoomJoinActionType = LobbyRTCRoomJoinActionType.AutomaticJoin, // ロビー入室時にどうボイチャに合流するか（デフォルト）

                // === 【ゲームの運用・マルチプラットフォーム設定】 ===
                DisableHostMigration = false,         // ホスト切断時に他の人に管理者権限を移す機能を「無効」にするか（false = 有効を推奨）
                RejoinAfterKickRequiresInvite = true, // キック（追放）されたプレイヤーが再入室する際に「招待」を必須にするか
                AllowedPlatformIds = null,            // 参加できるプラットフォーム（Steam/Switch等）の制限（nullは制限なし）
                CrossplayOptOut = false               // 他プラットフォームとのクロスプレイを拒否（オプトアウト）するか（false = クロスプレイを許可）
            };

            Debug.Log("EOSサーバーにロビー作成をリクエスト中...");

            // 4. EOSサーバーへロビー作成をリクエスト
            var lobbyUtcs = new UniTaskCompletionSource<string>(); // 
            lobbyInterface.CreateLobby(ref createLobbyOptions, null, (ref CreateLobbyCallbackInfo callbackInfo) =>
            {
                // サーバーから部屋の作成結果が返ってきたときのコールバック
                if (callbackInfo.ResultCode == Result.Success)
                {
                    Debug.Log("ロビー作成成功!");
                    // 成功：サーバー側で新規発行されたロビーID文字列（LobbyId）をセットして待機解除
                    lobbyUtcs.TrySetResult(callbackInfo.LobbyId);
                }
                else
                {
                    Debug.LogError($"ロビーの作成に失敗しました。 エラーコード: {callbackInfo.ResultCode}");
                    // 失敗：空文字をセットして待機解除
                    lobbyUtcs.TrySetResult(string.Empty);
                }
            });

            // サーバーからの処理結果が届くまで待機し、確定したロビーID（または空文字）を呼び出し元に返す
            return await lobbyUtcs.Task;
        }

        /// <summary>
        /// 検索（Search）によって発見したロビーの詳細情報ハンドルを指定して、そのロビーへ非同期で入室（ログイン）します。
        /// 成功時は入室したロビーのIDを返し、失敗時は空文字（string.Empty）を返します。
        /// </summary>
        public static async UniTask<string> JoinGameLobbyAsync(LobbyDetails lobbyDetailsHandle)
        {
            // 事前チェック：ターゲットとなるロビーのハンドル（住所情報）が渡されていない場合はエラー
            if (lobbyDetailsHandle == null)
            {
                Debug.LogError("入室対象のロビーハンドル（LobbyDetails）が指定されていません。");
                return string.Empty;
            }

            // 1. Lobby インターフェースを取得
            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

            // 2. 現在ログインが完了している自分の ProductUserId を取得・検証
            ProductUserId localProductUserId = EOSManager.Instance.GetProductUserId();
            if (localProductUserId == null || !localProductUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビーに入室できません。");
                return string.Empty;
            }

            // 3. ロビーに入室（合流）するための設定パラメーターを構築
            var joinLobbyOptions = new JoinLobbyOptions()
            {
                LobbyDetailsHandle = lobbyDetailsHandle, // 検索結果から取得した対象ロビーのハンドルを指定
                LocalUserId = localProductUserId,        // 入室する自分のID
                PresenceEnabled = false,                 // Epicフレンド機能へのロビー情報の同期設定（ホストと合わせてオフ）
                LocalRTCOptions = null,                  // ボイスチャットの初期オプション
                CrossplayOptOut = false,                 // クロスプレイを拒否するかどうか
                RTCRoomJoinActionType = LobbyRTCRoomJoinActionType.AutomaticJoin // ボイチャ自動参加の挙動設定
            };

            Debug.Log("EOSサーバーにロビー入室をリクエスト中...");
            // 4.EOSサーバーへロビーへの入室をリクエスト
            var joinUtcs = new UniTaskCompletionSource<string>(); // UniTask用待機ソース
            lobbyInterface.JoinLobby(ref joinLobbyOptions, null, (ref JoinLobbyCallbackInfo callbackInfo) =>
            {
                // サーバーから入室処理の結果が返ってきたときのコールバック
                if (callbackInfo.ResultCode == Result.Success)
                {
                    Debug.Log("ロビーへの入室に成功しました！");
                    // 成功：ゲスト側も入室時にロビーIDが手に入るため、これをセットして待機解除
                    joinUtcs.TrySetResult(callbackInfo.LobbyId);
                }
                else
                {
                    Debug.LogError($"ロビーへの入室に失敗しました。 エラーコード: {callbackInfo.ResultCode}");
                    // 失敗：空文字をセットして待機解除
                    joinUtcs.TrySetResult(string.Empty);
                }
            });

            // サーバーからの入室完了の返事が届くまで待機し、ロビーID（または空文字）を返す
            return await joinUtcs.Task;
        }

        /// <summary>
        /// 指定した「部屋名（合言葉）」に一致するプライベートロビーを非同期で検索します。
        /// 発見した場合はロビーの詳細ハンドル（LobbyDetails）を返し、見つからない場合は null を返します。
        /// </summary>
        public static async UniTask<LobbyDetails> SearchGameLobbyAsync(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogError("検索するロビー名を入力してください。");
                return null;
            }

            // 1. Lobbyインターフェースの取得
            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();
            ProductUserId localProductUserId = EOSManager.Instance.GetProductUserId();

            if (localProductUserId == null || !localProductUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビーを検索できません。");
                return null;
            }

            // 2. ロビー検索用のハンドル（LobbySearch）を作成するための設定
            var createLobbySearchOptions = new CreateLobbySearchOptions()
            {
                MaxResults = 10 // 一度に取得する検索結果の最大件数
            };

            // 検索オブジェクト（ハンドル）を生成
            Result createSearchResult = lobbyInterface.CreateLobbySearch(ref createLobbySearchOptions, out LobbySearch lobbySearchHandle);

            if (createSearchResult != Result.Success || lobbySearchHandle == null)
            {
                Debug.LogError($"LobbySearchハンドルの生成に失敗しました: {createSearchResult}");
                return null;
            }

            // 3. 検索フィルター条件の設定
            // 作成時に指定した BucketId ($"PrivateRoom_{roomName}") と完全に一致する部屋を探す条件を設定します
            var attributeFilterOptions = new LobbySearchSetParameterOptions()
            {
                Parameter = new AttributeData()
                {
                    Key = "BucketId", // 検索対象のキー（EOS標準のバケットID）
                    Value = $"PrivateRoom_{roomName}" // 作成時と同じ文字列
                },
                ComparisonOp = ComparisonOp.Equal // 「完全一致（==）」という条件を指定
            };

            // フィルター条件を検索ハンドルに登録
            lobbySearchHandle.SetParameter(ref attributeFilterOptions);

            Debug.Log($"合言葉 [PrivateRoom_{roomName}] でロビーを検索中...");

            var searchUtcs = new UniTaskCompletionSource<bool>();

            // 4. EOSサーバーに対して検索を正式に実行（非同期処理）
            var lobbySearchFindOptions = new LobbySearchFindOptions()
            {
                LocalUserId = localProductUserId
            };

            lobbySearchHandle.Find(ref lobbySearchFindOptions, null, (ref LobbySearchFindCallbackInfo callbackInfo) =>
            {
                if (callbackInfo.ResultCode == Result.Success)
                {
                    searchUtcs.TrySetResult(true);
                }
                else
                {
                    Debug.LogError($"ロビー検索に失敗しました。 エラーコード: {callbackInfo.ResultCode}");
                    searchUtcs.TrySetResult(false);
                }
            });

            // サーバーから検索結果のリストが届くまで待機
            bool isSearchSuccess = await searchUtcs.Task;

            if (isSearchSuccess)
            {
                // 5. 届いた検索結果リストから、0番目（最初に見つかった部屋）の詳細ハンドルを取得
                var searchGetResultCountOptions = new LobbySearchGetSearchResultCountOptions();
                uint resultCount = lobbySearchHandle.GetSearchResultCount(ref searchGetResultCountOptions);

                if (resultCount > 0)
                {
                    var searchCopyResultByIndexOptions = new LobbySearchCopySearchResultByIndexOptions()
                    {
                        LobbyIndex = 0 // 最初に見つかった1件を指定
                    };

                    // リストから部屋の詳細情報（LobbyDetailsハンドル）をコピーして抽出
                    Result copyResult = lobbySearchHandle.CopySearchResultByIndex(ref searchCopyResultByIndexOptions, out LobbyDetails foundLobbyDetails);

                    if (copyResult == Result.Success && foundLobbyDetails != null)
                    {
                        Debug.Log($"ロビーを発見しました！(件数: {resultCount})");
                        return foundLobbyDetails; // ★これを入室メソッドに渡す
                    }
                }
                else
                {
                    Debug.LogWarning("一致するロビーが見つかりませんでした。");
                }
            }

            return null;
        }
    }
}