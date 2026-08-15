using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
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
    ///  1. LobbyInterface を static readonly フィールドの即時初期化からプロパティによる遅延取得に変更。
    ///     EOSManager の初期化が完了する前にこのクラスへ最初にアクセスすると、静的コンストラクタが
    ///     例外を投げて TypeInitializationException となり、以降アプリ全体でこのクラスが恒久的に
    ///     使用不能になるため（C#の仕様上、静的コンストラクタの失敗は再試行されない）。
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
        /// LobbyInterface を必要になったタイミングで取得する。
        /// static readonly フィールドでの即時初期化は、EOSManager が未初期化の状態で
        /// このクラスに最初にアクセスした場合にクラス全体を使用不能にするリスクがあるため、
        /// 呼び出しの都度取得する方式に変更している。
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

        /// <summary>
        /// 通知の登録ID と、それに対応する解除処理をセットで保持するための内部クラス。
        /// 通知種別ごとに正しい Remove 系APIを呼べるようにするための仕組み。
        /// </summary>
        private sealed class NotificationHandle
        {
            public ulong Id;
            public Action<ulong> Unregister;
        }

        private static readonly Dictionary<LobbyNoticeEnum, NotificationHandle> lobbyNotificationHandles = new Dictionary<LobbyNoticeEnum, NotificationHandle>();

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
                        if (loginData.ResultCode == Result.Success)
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

            // 1. ロビー探索
            var createLobbySearchOptions = new CreateLobbySearchOptions() { MaxResults = 1 }; // 検索設定
            Result createSearchResult = lobbyInterface.CreateLobbySearch(ref createLobbySearchOptions, out LobbySearch lobbySearchHandle);
            if (createSearchResult != Result.Success || lobbySearchHandle == null)
            {
                Debug.LogError($"LobbySearchハンドルの生成に失敗しました: {createSearchResult}");
                return string.Empty;
            }

            string targetLobbyId = string.Empty;

            // lobbySearchHandle の確実な解放のために最外周を try-finally で囲う
            try
            {
                // 1.2. ロビー探索条件設定
                var attributeFilterOptions = new LobbySearchSetParameterOptions() // 部屋名
                {
                    Parameter = new AttributeData()
                    {
                        Key = ATTRIBUTE_LOBBY_ROOM_NAME,
                        Value = roomName
                    },
                    ComparisonOp = ComparisonOp.Equal
                };
                lobbySearchHandle.SetParameter(ref attributeFilterOptions);

                var searchUtcs = new UniTaskCompletionSource<bool>();
                var lobbySearchFindOptions = new LobbySearchFindOptions() { LocalUserId = localUserId };
                lobbySearchHandle.Find(ref lobbySearchFindOptions, null, (ref LobbySearchFindCallbackInfo callbackInfo) =>
                {
                    if (callbackInfo.ResultCode == Result.Success)
                    {
                        searchUtcs.TrySetResult(true);
                    }
                    else
                    {
                        Debug.LogWarning($"ロビー検索 Find コールバック結果: {callbackInfo.ResultCode}");
                        searchUtcs.TrySetResult(false);
                    }
                });

                bool isSearchSuccess = await searchUtcs.Task.AttachExternalCancellation(cancellationToken);

                if (isSearchSuccess) // 1.3. ロビーが存在する
                {
                    // 1.3.1 ロビー数カウント
                    var countOptions = new LobbySearchGetSearchResultCountOptions();
                    uint searchResultCount = lobbySearchHandle.GetSearchResultCount(ref countOptions);
                    if (searchResultCount > 0)
                    {
                        // 1.3.2 ロビー情報取得
                        Debug.Log("一致するロビーを発見しました");
                        var searchCopyResultByIndexOptions = new LobbySearchCopySearchResultByIndexOptions() { LobbyIndex = 0 };
                        Result copyResult = lobbySearchHandle.CopySearchResultByIndex(ref searchCopyResultByIndexOptions, out LobbyDetails foundLobbyDetails);

                        if (copyResult == Result.Success && foundLobbyDetails != null) // 1.3.3 ロビー入室
                        {
                            try
                            {
                                var joinLobbyOptions = new JoinLobbyOptions() // 入室設定
                                {
                                    LobbyDetailsHandle = foundLobbyDetails,
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
                                        Debug.Log("既存のロビーへの入室に成功しました！");
                                        joinUtcs.TrySetResult(callbackInfo.LobbyId);
                                    }
                                    else
                                    {
                                        Debug.LogError($"ロビーへの入室に失敗しました。エラーコード: {callbackInfo.ResultCode}");
                                        joinUtcs.TrySetResult(string.Empty);
                                    }
                                });

                                targetLobbyId = await joinUtcs.Task.AttachExternalCancellation(cancellationToken);
                            }
                            finally
                            {
                                foundLobbyDetails.Release();
                            }
                        }
                        else
                        {
                            Debug.LogError($"ロビー詳細ハンドルのコピーに失敗しました。 エラーコード: {copyResult}");
                        }
                    }
                }

                if (string.IsNullOrEmpty(targetLobbyId)) // 1.4. ロビーが存在しない
                {
                    // 注意: 「検索して無ければ作成」という流れのため、複数クライアントがほぼ同時に
                    // 同名ロビーを検索・未発見・作成した場合、同名ロビーが複数生成されるレースコンディションが
                    // 起こり得る。厳密な一意性が必要な場合はサーバー側での制御や作成失敗時の再検索を検討すること。

                    // 1.4.1 ロビー作成
                    var createLobbyOptions = new CreateLobbyOptions() // ロビー制作オプション
                    {
                        LocalUserId = localUserId,
                        MaxLobbyMembers = 8,
                        PermissionLevel = LobbyPermissionLevel.Publicadvertised,
                        PresenceEnabled = false,
                        AllowInvites = true,
                        BucketId = "PRIVATE_ROOM",
                        LobbyId = null,
                        EnableJoinById = true,
                        EnableRTCRoom = false,
                        LocalRTCOptions = null,
                        RTCRoomJoinActionType = LobbyRTCRoomJoinActionType.AutomaticJoin,
                        DisableHostMigration = false,
                        RejoinAfterKickRequiresInvite = true,
                        AllowedPlatformIds = null,
                        CrossplayOptOut = false
                    };
                    var createLobbyUtcs = new UniTaskCompletionSource<string>(); // UniTask用待機ソース
                    lobbyInterface.CreateLobby(ref createLobbyOptions, null, (ref CreateLobbyCallbackInfo callbackInfo) =>
                    {
                        if (callbackInfo.ResultCode == Result.Success)
                        {
                            Debug.Log("ロビーの新規作成に成功しました！(ホストとして開始)");
                            createLobbyUtcs.TrySetResult(callbackInfo.LobbyId);
                        }
                        else
                        {
                            Debug.LogError($"ロビーの作成に失敗しました。 エラーコード: {callbackInfo.ResultCode}");
                            createLobbyUtcs.TrySetResult(string.Empty);
                        }
                    });
                    targetLobbyId = await createLobbyUtcs.Task.AttachExternalCancellation(cancellationToken);

                    // 1.4.2 ロビー名登録
                    if (string.IsNullOrEmpty(targetLobbyId))
                    {
                        return string.Empty;
                    }
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
                                    Debug.Log("ロビーのカスタム属性のアップデートに成功しました！これで検索可能になります。");
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

                // 1.5. ユーザー名登録
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
            finally
            {
                // 何があっても LobbySearch ハンドルはここで確実に解放する
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


        #region ========== ロビー通知 ==========

        /// <summary>
        /// ロビーメンバー変更通知登録
        /// </summary>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <param name="lobbyNoticeEnum">通知管理用の識別キー</param>
        /// <param name="onMemberChangedCallback">通知コールバック</param>
        public static void RegisterLobbyNotifications(string lobbyId, LobbyNoticeEnum lobbyNoticeEnum, Action<LobbyMemberStatusReceivedCallbackInfo> onMemberChangedCallback)
        {
            var lobbyInterface = LobbyInterface;
            if (lobbyInterface == null)
            {
                Debug.LogError("Lobby Interface の取得に失敗しました。");
                return;
            }

            // 1. すでに登録されている場合は一度解除
            UnregisterLobbyNotifications(lobbyNoticeEnum);

            // 2. 通知のオプション設定
            var memberStatusOptions = new AddNotifyLobbyMemberStatusReceivedOptions();

            // 3. EOSサーバーに通知イベント（リスナー）を登録
            ulong notificationId = lobbyInterface.AddNotifyLobbyMemberStatusReceived(
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
            //    こうしておくことで、将来別の種類の通知（AddNotifyLobbyUpdateReceived等）を
            //    同じ辞書で管理するようになっても、UnregisterLobbyNotifications 側で
            //    常に正しい Remove 系APIが呼ばれることが保証される。
            if (notificationId != 0)
            {
                lobbyNotificationHandles[lobbyNoticeEnum] = new NotificationHandle
                {
                    Id = notificationId,
                    Unregister = id => LobbyInterface?.RemoveNotifyLobbyMemberStatusReceived(id)
                };
                Debug.Log($"ロビーメンバー変更通知を登録しました。Type: {lobbyNoticeEnum}, ID: {notificationId}");
            }
        }

        /// <summary>
        /// ロビー通知登録を解除
        /// </summary>
        /// <param name="lobbyNoticeEnum">解除したい通知管理用の識別キー</param>
        public static void UnregisterLobbyNotifications(LobbyNoticeEnum lobbyNoticeEnum)
        {
            // 辞書にキーが存在するか確認
            if (!lobbyNotificationHandles.TryGetValue(lobbyNoticeEnum, out NotificationHandle handle) || handle.Id == 0)
            {
                return; // 登録されていなければ何もしない
            }

            // 登録時に紐付けた解除処理を呼ぶことで、常に対応する種別のRemove系APIが実行される
            handle.Unregister?.Invoke(handle.Id);
            Debug.Log($"ロビー通知を解除しました。Type: {lobbyNoticeEnum}, ID: {handle.Id}");

            // 辞書から削除
            lobbyNotificationHandles.Remove(lobbyNoticeEnum);
        }

        /// <summary>
        /// 全てのロビー通知登録を解除 (シーン遷移やログアウト時に一括クリア用)
        /// </summary>
        public static void UnregisterAllNotifications()
        {
            foreach (var kvp in lobbyNotificationHandles)
            {
                if (kvp.Value.Id != 0)
                {
                    kvp.Value.Unregister?.Invoke(kvp.Value.Id);
                    Debug.Log($"ロビー通知を一括解除しました。Type: {kvp.Key}");
                }
            }
            lobbyNotificationHandles.Clear();
        }

        #endregion ========== ロビー通知 ==========
    }
}