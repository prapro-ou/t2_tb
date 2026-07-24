using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks; // UniTaskの非同期処理（async/await）を利用するために必要
using PlayEveryWare.EpicOnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices;

namespace OriginalNameSpace.EOSMethod.Lobby
{
    /// <summary>
    /// Epic Online Services (EOS) の各種手続き（認証・通信・ロビー）を管理する静的クラス
    /// </summary>
    /// <remarks>
    /// レビュー指摘に基づく主な変更点:
    ///  3. 通知登録を「通知ID + それに対応する解除処理」のペアで管理するように変更。
    ///     以前は解除処理が RemoveNotifyLobbyMemberStatusReceived に決め打ちされており、
    ///     将来別種の通知（例: ロビー更新通知）を同じ辞書で扱い始めた際に誤ったAPIを呼ぶ
    ///     バグを生みやすい構造だった。
    ///  4. GetLobbyMemberDisplayNames 内で LobbyDetails ハンドルを二重取得していたのを解消。
    ///  5. 主要な非同期メソッドに CancellationToken を追加し、シーン遷移等での中断に対応。
    /// </remarks>
    public static class EOSLobbyMethod
    {

        #region ========== 定義 ==========

        /// <summary>
        /// LobbyInterfaceを必要になったタイミングで取得する
        /// </summary>
        private static LobbyInterface LobbyInterface
        {
            get
            {
                var platformInterface = EOSManager.Instance?.GetEOSPlatformInterface();
                if (platformInterface == null)
                {
                    Debug.LogError("EOS Platform Interface の取得に失敗しました。EOSManagerの初期化完了後に呼び出してください。");
                    return null;
                }
                return platformInterface.GetLobbyInterface();
            }
        }

        private const string ATTRIBUTE_LOBBY_ROOM_NAME = "ROOM_NAME";
        private const string ATTRIBUTE_PLAYER_DISPLAY_NAME = "DISPLAY_NAME";
        private const string DEVICE_LOGIN_PLACEHOLDER_DISPLAY_NAME = "NULL";

        private static readonly Dictionary<ulong, Action<ulong>> lobbyNotificationHandles = new Dictionary<ulong, Action<ulong>>();

        #endregion ========== 定義 ==========


        #region ========== ログイン・ロビー入退出処理 ==========

        /// <summary>
        /// EOSログイン
        /// </summary>
        /// <param name="cancellationToken">処理を中断するためのトークン</param>
        /// <returns>ログイン成功時: ProductUserId 失敗時: null</returns>
        public static async UniTask<ProductUserId> LoginAsync(CancellationToken cancellationToken = default)
        {
            // 0. 初期確認
            var connectInterface = EOSManager.Instance.GetEOSConnectInterface();
            if (connectInterface == null)
            {
                Debug.LogError("Connect Interface の取得に失敗しました。");
                return null;
            }

            // 1. デバイスID取得
            var createDeviceIdOptions = new CreateDeviceIdOptions
            {
                DeviceModel = SystemInfo.deviceModel // 端末のモデル名
            };
            var deviceIdUtcs = new UniTaskCompletionSource<CreateDeviceIdCallbackInfo>(); // UniTask用待機ソース
            connectInterface.CreateDeviceId(ref createDeviceIdOptions, null, (ref CreateDeviceIdCallbackInfo createDeviceIdData) =>
            {
                deviceIdUtcs.TrySetResult(createDeviceIdData); // 結果返却
            });
            CreateDeviceIdCallbackInfo deviceIdResult = await deviceIdUtcs.Task.AttachExternalCancellation(cancellationToken);

            // 2. ログイン処理
            if (deviceIdResult.ResultCode == Result.Success || // ID作成成功
                deviceIdResult.ResultCode == Result.DuplicateNotAllowed) // ID作成済み
            {
                Debug.Log("デバイスIDの準備完了。StartConnectLoginWithOptions を実行します。");

                var loginOptions = new LoginOptions
                {
                    Credentials = new Credentials
                    {
                        Type = ExternalCredentialType.DeviceidAccessToken, // 認証タイプ指定
                        Token = null // アクセストークン(DeviceIdならnull)
                    },
                    UserLoginInfo = new UserLoginInfo
                    {
                        DisplayName = DEVICE_LOGIN_PLACEHOLDER_DISPLAY_NAME // ユーザー名（DeviceId認証では未使用のプレースホルダー）
                    }
                };
                var loginUtcs = new UniTaskCompletionSource<ProductUserId>(); // UniTask用待機ソース
                EOSManager.Instance.StartConnectLoginWithOptions( // EOS Connect へのログインリクエスト
                    loginOptions,
                    (LoginCallbackInfo loginData) =>
                    {
                        // ログイン処理の結果が返ってきたときのコールバック
                        if (loginData.ResultCode == Result.Success || loginData.ResultCode == Result.AlreadyPending)
                        {
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
                return await loginUtcs.Task.AttachExternalCancellation(cancellationToken);
            }
            else
            {
                // デバイスIDの作成自体に失敗した場合はエラーログを出して終了
                Debug.LogError($"デバイスIDの作成・取得に失敗しました: {deviceIdResult.ResultCode}");
                return null;
            }
        }

        /// <summary>
        /// EOSログアウト
        /// </summary>
        /// <param name="cancellationToken">処理を中断するためのトークン</param>
        /// <returns>ログアウト成功時: true 失敗時: false</returns>
        public static async UniTask<bool> LogoutAsync(CancellationToken cancellationToken = default)
        {
            // 0. 初期確認
            ProductUserId localUserId = EOSManager.Instance.GetProductUserId();
            if (localUserId == null || !localUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビー処理を実行できません。");
                return false;
            }
            var connectInterface = EOSManager.Instance.GetEOSConnectInterface();
            if (connectInterface == null)
            {
                Debug.LogError("Connect Interface の取得に失敗しました。");
                return false;
            }

            // 1. ログアウト処理
            var logoutOptions = new LogoutOptions
            {
                LocalUserId = localUserId
            };
            var logoutUtcs = new UniTaskCompletionSource<bool>(); // UniTask用待機ソース
            connectInterface.Logout(ref logoutOptions, null, (ref LogoutCallbackInfo logoutData) =>
            {
                if (logoutData.ResultCode == Result.Success)
                {
                    Debug.Log("EOS ログアウトに成功しました。");
                    UnregisterAllNotifications();
                    logoutUtcs.TrySetResult(true);
                }
                else
                {
                    Debug.LogError($"EOS ログアウトに失敗しました。ステータスコード: {logoutData.ResultCode}");
                    logoutUtcs.TrySetResult(false);
                }
            });

            return await logoutUtcs.Task.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>
        /// 部屋名からEOS Lobby用の決定論的なLobbyIdを生成する。
        /// </summary>
        private const int MAX_LOBBY_ID_LENGTH = 64;

        private static string BuildDeterministicLobbyId(string roomName)
        {
            // 1. サニタイズ
            string sanitized = System.Text.RegularExpressions.Regex.Replace(roomName, @"[^a-zA-Z0-9_-]", "_");

            // 2. 決定論的なハッシュの生成（先頭4byte = 8文字）
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(roomName));
                string hashSuffix = BitConverter.ToString(hashBytes, 0, 4).Replace("-", "").ToLowerInvariant();

                // 3. ハッシュを考慮して、サニタイズ側を事前に切り詰める
                // 最大長(64) - アンダースコア(1) - ハッシュ長(8) = 55文字
                int maxSanitizedLength = MAX_LOBBY_ID_LENGTH - hashSuffix.Length - 1;

                if (sanitized.Length > maxSanitizedLength)
                {
                    sanitized = sanitized.Substring(0, maxSanitizedLength);
                }

                // 4. 結合（確実に64文字以下になり、末尾に必ずハッシュが残る）
                return $"{sanitized}_{hashSuffix}";
            }
        }

        /// <summary>
        /// ロビー参加・作成および表示名の自動登録
        /// </summary>
        /// <param name="roomName">合流・作成ロビーの部屋名</param>
        /// <param name="displayName">ロビーに登録する自身の表示名</param>
        /// <param name="cancellationToken">処理を中断するためのトークン</param>
        /// <returns>成功時:ロビーID 失敗時:空文字</returns>
        public static async UniTask<string> JoinOrCreateGameLobbyWithDisplayNameAsync(string roomName, string displayName, CancellationToken cancellationToken = default)
        {
            // 0. 初期確認
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogError("ロビー名を入力してください。");
                return string.Empty;
            }
            ProductUserId localUserId = EOSManager.Instance.GetProductUserId();
            if (localUserId == null || !localUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビー処理を実行できません。");
                return string.Empty;
            }
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                return string.Empty;
            }

            string deterministicLobbyId = BuildDeterministicLobbyId(roomName);
            string targetLobbyId = string.Empty;
            bool isHost = false;

            // 1. 自分がホストになる前提で、LobbyIdを明示指定して作成を試みる（楽観的作成）
            var createLobbyOptions = new CreateLobbyOptions() // ロビー制作オプション
            {
                LocalUserId = localUserId,
                MaxLobbyMembers = 8,
                PermissionLevel = LobbyPermissionLevel.Publicadvertised,
                PresenceEnabled = false,
                AllowInvites = true,
                BucketId = "PRIVATE_ROOM",
                LobbyId = deterministicLobbyId, // ★ 部屋名から導出した固定IDを明示指定（一意性チェックをEOSサーバーに委ねる）
                EnableJoinById = true,
                EnableRTCRoom = false,
                LocalRTCOptions = null,
                RTCRoomJoinActionType = LobbyRTCRoomJoinActionType.AutomaticJoin,
                DisableHostMigration = false,
                RejoinAfterKickRequiresInvite = true,
                AllowedPlatformIds = null,
                CrossplayOptOut = false
            };
            var createUtcs = new UniTaskCompletionSource<CreateLobbyCallbackInfo>(); // UniTask用待機ソース
            lobbyInterface.CreateLobby(ref createLobbyOptions, null, (ref CreateLobbyCallbackInfo callbackInfo) =>
            {
                createUtcs.TrySetResult(callbackInfo);
            });
            CreateLobbyCallbackInfo createResult = await createUtcs.Task.AttachExternalCancellation(cancellationToken);

            if (createResult.ResultCode == Result.Success)
            {
                Debug.Log($"ロビーの新規作成に成功しました！(ホストとして開始) LobbyId: {createResult.LobbyId}");
                targetLobbyId = createResult.LobbyId;
                isHost = true;
            }
            else if (createResult.ResultCode == Result.RoomAlreadyExists)
            {
                // 既に他クライアントが同じLobbyIdでの作成に成功している → 参加側に回る
                Debug.Log($"部屋名 [{roomName}] のロビーは既に他クライアントが作成済みのため、参加を試みます。");

                // 作成直後は検索結果への反映に若干のタイムラグがある場合があるため、軽くリトライする
                const int maxJoinRetry = 5;
                for (int attempt = 0; attempt < maxJoinRetry && string.IsNullOrEmpty(targetLobbyId); attempt++)
                {
                    if (attempt > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromMilliseconds(500), cancellationToken: cancellationToken);
                    }
                    targetLobbyId = await JoinLobbyByIdAsync(lobbyInterface, localUserId, deterministicLobbyId, cancellationToken);
                }

                if (string.IsNullOrEmpty(targetLobbyId))
                {
                    Debug.LogError($"既存ロビー [{deterministicLobbyId}] への参加にすべて失敗しました。");
                    return string.Empty;
                }
            }
            else
            {
                Debug.LogError($"ロビーの作成に失敗しました。エラーコード: {createResult.ResultCode}");
                return string.Empty;
            }

            // 2. 自分がホストの場合のみ、UI表示や属性検索用に部屋名属性を登録する
            //    （参加検索自体はLobbyIdで直接行うため必須ではないが、既存機能との互換のために維持）
            if (isHost)
            {
                var updateLobbyModificationOptions = new UpdateLobbyModificationOptions() // ロビー更新オプション
                {
                    LobbyId = targetLobbyId,
                    LocalUserId = localUserId
                };
                Result modificationResult = lobbyInterface.UpdateLobbyModification(ref updateLobbyModificationOptions, out LobbyModification lobbyModificationHandle);
                if (modificationResult == Result.Success && lobbyModificationHandle != null)
                {
                    try
                    {
                        var attributeData = new AttributeData()
                        {
                            Key = ATTRIBUTE_LOBBY_ROOM_NAME,
                            Value = roomName
                        };
                        var lobbyModificationAddAttributeOptions = new LobbyModificationAddAttributeOptions()
                        {
                            Attribute = attributeData,
                            Visibility = LobbyAttributeVisibility.Public
                        };
                        lobbyModificationHandle.AddAttribute(ref lobbyModificationAddAttributeOptions);
                        var updateLobbyOptions = new UpdateLobbyOptions()
                        {
                            LobbyModificationHandle = lobbyModificationHandle
                        };
                        var updateLobbyUtcs = new UniTaskCompletionSource<bool>();
                        lobbyInterface.UpdateLobby(ref updateLobbyOptions, null, (ref UpdateLobbyCallbackInfo callbackInfo) =>
                        {
                            if (callbackInfo.ResultCode == Result.Success)
                            {
                                Debug.Log("ロビーのカスタム属性のアップデートに成功しました！");
                                updateLobbyUtcs.TrySetResult(true);
                            }
                            else
                            {
                                Debug.LogError($"ロビーのカスタム属性アップデートに失敗しました。 エラーコード: {callbackInfo.ResultCode}");
                                updateLobbyUtcs.TrySetResult(false);
                            }
                        });
                        await updateLobbyUtcs.Task.AttachExternalCancellation(cancellationToken);
                    }
                    finally
                    {
                        lobbyModificationHandle.Release();
                    }
                }
                else
                {
                    Debug.LogError($"LobbyModificationハンドルの取得に失敗しました: {modificationResult}");
                }
            }

            // 3. ユーザー名登録（ホスト・参加者共通）
            if (!string.IsNullOrEmpty(targetLobbyId) && !string.IsNullOrEmpty(displayName))
            {
                var updateOptions = new UpdateLobbyModificationOptions()
                {
                    LobbyId = targetLobbyId,
                    LocalUserId = localUserId
                };
                Result memberModResult = lobbyInterface.UpdateLobbyModification(ref updateOptions, out LobbyModification memberModificationHandle);
                if (memberModResult == Result.Success && memberModificationHandle != null)
                {
                    try
                    {
                        var memberAttributeData = new AttributeData()
                        {
                            Key = ATTRIBUTE_PLAYER_DISPLAY_NAME,
                            Value = displayName
                        };
                        var addMemberAttributeOptions = new LobbyModificationAddMemberAttributeOptions()
                        {
                            Attribute = memberAttributeData,
                            Visibility = LobbyAttributeVisibility.Public
                        };
                        Result addResult = memberModificationHandle.AddMemberAttribute(ref addMemberAttributeOptions);
                        if (addResult == Result.Success)
                        {
                            var updateLobbyOptions = new UpdateLobbyOptions() { LobbyModificationHandle = memberModificationHandle };
                            var memberUpdateUtcs = new UniTaskCompletionSource<bool>();

                            lobbyInterface.UpdateLobby(ref updateLobbyOptions, null, (ref UpdateLobbyCallbackInfo callbackInfo) =>
                            {
                                memberUpdateUtcs.TrySetResult(callbackInfo.ResultCode == Result.Success);
                            });

                            bool isNameUpdateSuccess = await memberUpdateUtcs.Task.AttachExternalCancellation(cancellationToken);
                            if (isNameUpdateSuccess)
                            {
                                Debug.Log("自身の表示名の登録に成功しました。");
                            }
                            else
                            {
                                Debug.LogError("自身の表示名のサーバー反映に失敗しました。");
                            }
                        }
                        else
                        {
                            Debug.LogError($"メンバー属性の追加に失敗: {addResult}");
                        }
                    }
                    finally
                    {
                        memberModificationHandle.Release();
                    }
                }
                else
                {
                    Debug.LogError($"表示名登録用のLobbyModificationの取得に失敗: {memberModResult}");
                }
            }

            return targetLobbyId;
        }

        /// <summary>
        /// 既知のLobbyIdを直接指定してロビーを検索・参加する。
        /// </summary>
        private static async UniTask<string> JoinLobbyByIdAsync(LobbyInterface lobbyInterface, ProductUserId localUserId, string lobbyId, CancellationToken cancellationToken)
        {
            var createSearchOptions = new CreateLobbySearchOptions() { MaxResults = 1 };
            Result createSearchResult = lobbyInterface.CreateLobbySearch(ref createSearchOptions, out LobbySearch lobbySearchHandle);
            if (createSearchResult != Result.Success || lobbySearchHandle == null)
            {
                Debug.LogError($"LobbySearchハンドルの生成に失敗しました: {createSearchResult}");
                return string.Empty;
            }

            try
            {
                var setLobbyIdOptions = new LobbySearchSetLobbyIdOptions() { LobbyId = lobbyId };
                lobbySearchHandle.SetLobbyId(ref setLobbyIdOptions);

                var findOptions = new LobbySearchFindOptions() { LocalUserId = localUserId };
                var findUtcs = new UniTaskCompletionSource<bool>();
                lobbySearchHandle.Find(ref findOptions, null, (ref LobbySearchFindCallbackInfo callbackInfo) =>
                {
                    findUtcs.TrySetResult(callbackInfo.ResultCode == Result.Success);
                });

                bool isFound = await findUtcs.Task.AttachExternalCancellation(cancellationToken);
                if (!isFound)
                {
                    Debug.LogWarning($"LobbyId [{lobbyId}] によるロビー検索に失敗しました。");
                    return string.Empty;
                }

                var countOptions = new LobbySearchGetSearchResultCountOptions();
                uint resultCount = lobbySearchHandle.GetSearchResultCount(ref countOptions);
                if (resultCount == 0)
                {
                    Debug.LogWarning($"LobbyId [{lobbyId}] のロビーがまだ検索に反映されていません（作成直後の反映待ちの可能性）。");
                    return string.Empty;
                }

                var copySearchOptions = new LobbySearchCopySearchResultByIndexOptions() { LobbyIndex = 0 };
                Result copyResult = lobbySearchHandle.CopySearchResultByIndex(ref copySearchOptions, out LobbyDetails lobbyDetails);
                if (copyResult != Result.Success || lobbyDetails == null)
                {
                    Debug.LogError($"ロビー詳細ハンドルのコピーに失敗しました。エラーコード: {copyResult}");
                    return string.Empty;
                }

                try
                {
                    var joinLobbyOptions = new JoinLobbyOptions()
                    {
                        LobbyDetailsHandle = lobbyDetails,
                        LocalUserId = localUserId,
                        PresenceEnabled = false,
                        LocalRTCOptions = null,
                        CrossplayOptOut = false,
                        RTCRoomJoinActionType = LobbyRTCRoomJoinActionType.AutomaticJoin
                    };
                    var joinUtcs = new UniTaskCompletionSource<string>();
                    lobbyInterface.JoinLobby(ref joinLobbyOptions, null, (ref JoinLobbyCallbackInfo callbackInfo) =>
                    {
                        if (callbackInfo.ResultCode == Result.Success)
                        {
                            Debug.Log("既存のロビーへの入室に成功しました（LobbyId指定による参加）。");
                            joinUtcs.TrySetResult(callbackInfo.LobbyId);
                        }
                        else
                        {
                            Debug.LogError($"ロビーへの入室に失敗しました。エラーコード: {callbackInfo.ResultCode}");
                            joinUtcs.TrySetResult(string.Empty);
                        }
                    });

                    return await joinUtcs.Task.AttachExternalCancellation(cancellationToken);
                }
                finally
                {
                    lobbyDetails.Release();
                }
            }
            finally
            {
                lobbySearchHandle.Release();
            }
        }

        /// <summary>
        /// 指定したロビーから退室します。
        /// </summary>
        /// <param name="lobbyId">退室するロビーID</param>
        /// <param name="cancellationToken">処理を中断するためのトークン</param>
        /// <returns>成功時: true 失敗時: false</returns>
        public static async UniTask<bool> LeaveLobbyAsync(string lobbyId, CancellationToken cancellationToken = default)
        {
            // 0. 初期確認
            ProductUserId localUserId = EOSManager.Instance.GetProductUserId();
            if (localUserId == null || !localUserId.IsValid())
            {
                Debug.LogError("有効な ProductUserId が指定されていないため、退室できません。");
                return false;
            }
            if (string.IsNullOrEmpty(lobbyId))
            {
                Debug.LogError("ロビーIDが空のため、退室できません。");
                return false;
            }
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                return false;
            }

            // 1. 退出処理
            var leaveLobbyOptions = new LeaveLobbyOptions() // 退室設定
            {
                LocalUserId = localUserId,
                LobbyId = lobbyId
            };
            var utcs = new UniTaskCompletionSource<bool>();
            lobbyInterface.LeaveLobby(ref leaveLobbyOptions, null, (ref LeaveLobbyCallbackInfo callbackInfo) =>
            {
                if (callbackInfo.ResultCode == Result.Success)
                {
                    Debug.Log($"ロビー [{lobbyId}] から正常に退室しました。");
                    utcs.TrySetResult(true);
                }
                else
                {
                    Debug.LogError($"ロビーからの退室に失敗しました。エラーコード: {callbackInfo.ResultCode}");
                    utcs.TrySetResult(false);
                }
            });

            return await utcs.Task.AttachExternalCancellation(cancellationToken);
        }

        #endregion ========== ログイン・ロビー入退出処理 ==========

        #region ========== ロビー情報取得 ==========

        /// <summary>
        /// ロビー内メンバー取得
        /// </summary>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <returns>ロビーメンバーのProductUserIdリスト</returns>
        public static List<ProductUserId> GetLobbyMembers(string lobbyId)
        {
            var list = new List<ProductUserId>();
            ProductUserId localUserId = EOSManager.Instance.GetProductUserId();
            if (localUserId == null || !localUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビー処理を実行できません。");
                return list;
            }
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                return list;
            }

            // ロビー詳細を取得
            var copyOptions = new CopyLobbyDetailsHandleOptions()
            {
                LobbyId = lobbyId,
                LocalUserId = localUserId
            };

            Result result = lobbyInterface.CopyLobbyDetailsHandle(ref copyOptions, out LobbyDetails lobbyDetails);

            // 完全に取得失敗し、ハンドルも生成されていない場合は空リストを返す
            if (result != Result.Success || lobbyDetails == null)
            {
                Debug.LogError($"ロビー詳細の取得に失敗しました: {result}");
                return list;
            }

            try
            {
                return GetLobbyMembersFromDetails(lobbyDetails);
            }
            finally
            {
                // ハンドル解放
                lobbyDetails.Release();
            }
        }

        /// <summary>
        /// 取得済みの LobbyDetails ハンドルからメンバー一覧を取得する内部処理。
        /// GetLobbyMemberDisplayNames から呼ぶ際に LobbyDetails の二重取得を避けるために分離している。
        /// </summary>
        private static List<ProductUserId> GetLobbyMembersFromDetails(LobbyDetails lobbyDetails)
        {
            var list = new List<ProductUserId>();
            var countOptions = new LobbyDetailsGetMemberCountOptions();
            uint memberCount = lobbyDetails.GetMemberCount(ref countOptions);

            for (uint i = 0; i < memberCount; i++)
            {
                var memberOptions = new LobbyDetailsGetMemberByIndexOptions() { MemberIndex = i };
                ProductUserId memberPuid = lobbyDetails.GetMemberByIndex(ref memberOptions);
                if (memberPuid != null && memberPuid.IsValid())
                {
                    list.Add(memberPuid);
                }
            }

            return list;
        }

        /// <summary>
        /// ロビーのメンバー属性から全員の表示名を取得する
        /// </summary>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <returns>表示名一覧</returns>
        public static Dictionary<ProductUserId, string> GetLobbyMemberDisplayNames(string lobbyId)
        {
            var displayNamesMap = new Dictionary<ProductUserId, string>();
            ProductUserId localUserId = EOSManager.Instance.GetProductUserId();
            if (localUserId == null || !localUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビー処理を実行できません。");
                return displayNamesMap;
            }
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                return displayNamesMap;
            }

            var copyOptions = new CopyLobbyDetailsHandleOptions()
            {
                LobbyId = lobbyId,
                LocalUserId = localUserId
            };

            if (lobbyInterface.CopyLobbyDetailsHandle(ref copyOptions, out LobbyDetails lobbyDetails) != Result.Success || lobbyDetails == null)
            {
                return displayNamesMap;
            }

            try
            {
                // 既に取得済みの lobbyDetails ハンドルを再利用してメンバー一覧を取得（二重取得の解消）
                var members = GetLobbyMembersFromDetails(lobbyDetails);
                foreach (var memberPuid in members)
                {
                    var getAttributeOptions = new LobbyDetailsCopyMemberAttributeByKeyOptions()
                    {
                        TargetUserId = memberPuid,
                        AttrKey = ATTRIBUTE_PLAYER_DISPLAY_NAME
                    };

                    // メンバー属性から名前をコピー
                    if (lobbyDetails.CopyMemberAttributeByKey(ref getAttributeOptions, out Epic.OnlineServices.Lobby.Attribute? memberAttribute) == Result.Success && memberAttribute != null)
                    {
                        if (memberAttribute.Value.Data != null && memberAttribute.Value.Data.Value.Value.AsUtf8 != null)
                        {
                            displayNamesMap[memberPuid] = memberAttribute.Value.Data.Value.Value.AsUtf8;
                        }
                        else
                        {
                            displayNamesMap[memberPuid] = "Unknown Player";
                        }
                    }
                    else
                    {
                        displayNamesMap[memberPuid] = "Unknown Player";
                    }
                }
            }
            finally
            {
                // ロビー詳細ハンドルを確実に解放
                lobbyDetails.Release();
            }

            return displayNamesMap;
        }

        /// <summary>
        /// ロビーのホスト（オーナー）の ProductUserId を取得する
        /// </summary>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <returns>ホストのProductUserId（取得失敗時は null）</returns>
        public static ProductUserId GetLobbyHostPuid(string lobbyId)
        {
            ProductUserId localUserId = EOSManager.Instance.GetProductUserId();
            if (localUserId == null || !localUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビー処理を実行できません。");
                return null;
            }
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                return null;
            }

            // ロビー詳細ハンドルを取得するためのオプションを設定
            var copyOptions = new CopyLobbyDetailsHandleOptions()
            {
                LobbyId = lobbyId,
                LocalUserId = localUserId
            };

            // ロビー詳細のコピー
            Result result = lobbyInterface.CopyLobbyDetailsHandle(ref copyOptions, out LobbyDetails lobbyDetails);

            if (result != Result.Success || lobbyDetails == null)
            {
                Debug.LogError($"ホスト取得用のロビー詳細のコピーに失敗しました: {result}");
                return null;
            }

            try
            {
                // ロビー詳細からオーナー（ホスト）の PUID を取得するオプションを設定
                var getOwnerOptions = new LobbyDetailsGetLobbyOwnerOptions();

                // ホストの ProductUserId を取得
                ProductUserId hostPuid = lobbyDetails.GetLobbyOwner(ref getOwnerOptions);

                if (hostPuid == null || !hostPuid.IsValid())
                {
                    Debug.LogWarning("ロビーのホストPUIDが有効ではありません。");
                    return null;
                }

                return hostPuid;
            }
            finally
            {
                // ハンドルを確実に解放
                lobbyDetails.Release();
            }
        }

        #endregion ========== ロビー情報取得 ==========

        #region ========== ロビー操作 ==========

        /// <summary>
        /// ロビーの許可レベル（侵入・公開設定）を変更します。
        /// </summary>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <param name="permissionLevel">設定する許可レベル（PublicAdvertised, JoinViaPresence, InviteOnly など）</param>
        /// <param name="cancellationToken">処理を中断するためのトークン</param>
        /// <returns>変更成功時: true 失敗時: false</returns>
        public static async UniTask<bool> UpdateLobbyPermissionLevelAsync(string lobbyId, LobbyPermissionLevel permissionLevel, CancellationToken cancellationToken = default)
        {
            // 0. 初期確認
            if (string.IsNullOrEmpty(lobbyId))
            {
                Debug.LogError("ロビーIDが空のため、設定を変更できません。");
                return false;
            }
            ProductUserId localUserId = EOSManager.Instance.GetProductUserId();
            if (localUserId == null || !localUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビー設定を変更できません。");
                return false;
            }
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                return false;
            }

            // 1. ロビーの修正用ハンドルの取得
            var updateLobbyModificationOptions = new UpdateLobbyModificationOptions()
            {
                LobbyId = lobbyId,
                LocalUserId = localUserId
            };

            Result modificationResult = lobbyInterface.UpdateLobbyModification(ref updateLobbyModificationOptions, out LobbyModification lobbyModificationHandle);
            if (modificationResult != Result.Success || lobbyModificationHandle == null)
            {
                Debug.LogError($"LobbyModificationハンドルの取得に失敗しました: {modificationResult}");
                return false;
            }

            try
            {
                var setPermissionOptions = new LobbyModificationSetPermissionLevelOptions
                {
                    PermissionLevel = permissionLevel
                };
                // 2. 許可レベル（侵入設定）の変更をハンドルに適用
                Result setPermissionResult = lobbyModificationHandle.SetPermissionLevel(ref setPermissionOptions);

                if (setPermissionResult != Result.Success)
                {
                    Debug.LogError($"許可レベルの設定に失敗しました: {setPermissionResult}");
                    return false;
                }

                // 3. サーバーへの変更適用リクエスト
                var updateLobbyOptions = new UpdateLobbyOptions()
                {
                    LobbyModificationHandle = lobbyModificationHandle
                };

                var updateLobbyUtcs = new UniTaskCompletionSource<bool>();
                lobbyInterface.UpdateLobby(ref updateLobbyOptions, null, (ref UpdateLobbyCallbackInfo callbackInfo) =>
                {
                    if (callbackInfo.ResultCode == Result.Success)
                    {
                        Debug.Log($"ロビーの許可レベルを [{permissionLevel}] に変更しました。");
                        updateLobbyUtcs.TrySetResult(true);
                    }
                    else
                    {
                        Debug.LogError($"ロビー設定のサーバー反映に失敗しました。 エラーコード: {callbackInfo.ResultCode}");
                        updateLobbyUtcs.TrySetResult(false);
                    }
                });

                return await updateLobbyUtcs.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                // ハンドルの確実な解放
                lobbyModificationHandle.Release();
            }
        }
        #endregion ========== ロビー操作 ==========

        #region ========== ロビー通知 ==========

        /// <summary>
        /// ロビーメンバー変更通知登録
        /// </summary>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <param name="lobbyNoticeEnum">通知管理用の識別キー</param>
        /// <param name="onMemberChangedCallback">通知コールバック</param>
        /// <returns>通知ID</returns>
        public static ulong RegisterLobbyNotifications(string lobbyId, Action<LobbyMemberStatusReceivedCallbackInfo> onMemberChangedCallback)
        {
            ulong notificationId = 0;
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                Debug.LogError("Lobby Interface の取得に失敗しました。");
                return 0;
            }

            // 2. 通知のオプション設定
            var memberStatusOptions = new AddNotifyLobbyMemberStatusReceivedOptions();

            // 3. EOSサーバーに通知イベント（リスナー）を登録
            notificationId = lobbyInterface.AddNotifyLobbyMemberStatusReceived(
                ref memberStatusOptions,
                null,
                (ref LobbyMemberStatusReceivedCallbackInfo callbackInfo) =>
                {
                    // 自分が対象のロビーに対する通知か確認
                    if (callbackInfo.LobbyId != lobbyId) return;

                    Debug.Log($"ロビーメンバーに変化がありました。対象PUID: {callbackInfo.TargetUserId}, 状態: {callbackInfo.CurrentStatus}");

                    onMemberChangedCallback?.Invoke(callbackInfo);
                }
            );

            // 4. 取得したIDと、対応する解除処理をセットで辞書に保存する。
            if (notificationId != 0)
            {
                lobbyNotificationHandles[notificationId] = action => LobbyInterface?.RemoveNotifyLobbyMemberStatusReceived(notificationId);
                Debug.Log($"ロビーメンバー変更通知を登録しました。");
            }
            return notificationId;
        }

        /// <summary>
        /// ロビー通知登録を解除
        /// </summary>
        /// <param name="lobbyNoticeEnum">解除したい通知管理用の識別キー</param>
        public static void UnregisterLobbyNotifications(ulong notificationId)
        {
            // 辞書にキーが存在するか確認
            if (!lobbyNotificationHandles.TryGetValue(notificationId, out var handle))
            {
                return; // 登録されていなければ何もしない
            }
            handle?.Invoke(notificationId);
            Debug.Log($"ロビー通知を解除しました。ID: {notificationId}");
            lobbyNotificationHandles.Remove(notificationId);
        }

        /// <summary>
        /// 全てのロビー通知登録を解除 (シーン遷移やログアウト時に一括クリア用)
        /// </summary>
        public static void UnregisterAllNotifications()
        {
            foreach (var handle in lobbyNotificationHandles)
            {
                handle.Value?.Invoke(handle.Key);
            }
            lobbyNotificationHandles.Clear();
        }

        #endregion ========== ロビー通知 ==========

    }
}