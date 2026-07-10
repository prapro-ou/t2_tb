using System;
using System.Collections.Generic;
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
    public static class EOSLobbyMethod
    {
        // === 定数定義 === //
        private const string ATTRIBUTE_LOBBY_ROOM_NAME = "ROOM_NAME";
        private const string ATTRIBUTE_PLAYER_DISPLAY_NAME = "DISPLAY_NAME";

        /// <summary>
        /// EOSログイン
        /// </summary>
        /// <returns>ログイン成功時: ProductUserId 失敗時: null</returns>
        public static async UniTask<ProductUserId> LoginAsync()
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
                deviceIdResult.ResultCode == Result.DuplicateNotAllowed) // ID作成済み
            {
                Debug.Log("デバイスIDの準備完了。StartConnectLoginWithOptions を実行します。");

                // 5.1.ログインオプションを構築
                var loginOptions = new LoginOptions
                {
                    Credentials = new Credentials
                    {
                        Type = ExternalCredentialType.DeviceidAccessToken, // 認証タイプ指定
                        Token = null // アクセストークン(DeviceIdならnull)
                    },
                    UserLoginInfo = new UserLoginInfo
                    {
                        DisplayName = "NULL" // ユーザー名
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
                Debug.LogError($"デバイスIDの作成・取得に失敗しました: {deviceIdResult.ResultCode}");
                return null;
            }
        }

        /// <summary>
        /// ロビー参加・作成および表示名の自動登録
        /// </summary>
        /// <param name="roomName">合流・作成ロビーの部屋名</param>
        /// <param name="displayName">ロビーに登録する自身の表示名</param>
        /// <returns>成功時:ロビーID 失敗時:空文字</returns>
        public static async UniTask<string> JoinOrCreateGameLobbyWithDisplayNameAsync(string roomName, string displayName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogError("ロビー名を入力してください。");
                return string.Empty;
            }

            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

            ProductUserId localProductUserId = EOSManager.Instance.GetProductUserId();
            if (localProductUserId == null || !localProductUserId.IsValid())
            {
                Debug.LogError("ログインしていないため、ロビー処理を実行できません。");
                return string.Empty;
            }

            var createLobbySearchOptions = new CreateLobbySearchOptions() { MaxResults = 1 };
            Result createSearchResult = lobbyInterface.CreateLobbySearch(ref createLobbySearchOptions, out LobbySearch lobbySearchHandle);
            if (createSearchResult != Result.Success || lobbySearchHandle == null)
            {
                Debug.LogError($"LobbySearchハンドルの生成に失敗しました: {createSearchResult}");
                return string.Empty;
            }

            string targetLobbyId = string.Empty;

            try
            {
                // バケットIDの設定（同じ仕組みのロビーが所属する大枠の空間）
                string bucketId = $"PRIVATE_ROOM";

                // 検索フィルターにカスタム属性を指定
                var attributeFilterOptions = new LobbySearchSetParameterOptions()
                {
                    Parameter = new AttributeData()
                    {
                        Key = ATTRIBUTE_LOBBY_ROOM_NAME,
                        Value = roomName
                    },
                    ComparisonOp = ComparisonOp.Equal
                };
                lobbySearchHandle.SetParameter(ref attributeFilterOptions);

                Debug.Log($"部屋名 [{roomName}] で既存のロビーを検索中...");
                var searchUtcs = new UniTaskCompletionSource<bool>();
                var lobbySearchFindOptions = new LobbySearchFindOptions() { LocalUserId = localProductUserId };

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

                bool isSearchSuccess = await searchUtcs.Task;

                if (isSearchSuccess)
                {
                    var countOptions = new LobbySearchGetSearchResultCountOptions();
                    uint searchResultCount = lobbySearchHandle.GetSearchResultCount(ref countOptions);

                    if (searchResultCount > 0)
                    {
                        Debug.Log($"一致するロビーを発見しました");

                        var searchCopyResultByIndexOptions = new LobbySearchCopySearchResultByIndexOptions() { LobbyIndex = 0 };
                        Result copyResult = lobbySearchHandle.CopySearchResultByIndex(ref searchCopyResultByIndexOptions, out LobbyDetails foundLobbyDetails);

                        if (copyResult == Result.Success && foundLobbyDetails != null)
                        {
                            try
                            {
                                var joinLobbyOptions = new JoinLobbyOptions()
                                {
                                    LobbyDetailsHandle = foundLobbyDetails,
                                    LocalUserId = localProductUserId,
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

                                targetLobbyId = await joinUtcs.Task;
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

                // 既存のロビーが見つからず、まだ入室できていない場合は新規作成
                if (string.IsNullOrEmpty(targetLobbyId))
                {
                    Debug.Log($"部屋名 [{roomName}] に一致するロビーがなかったため、新しく作成します...");

                    var createLobbyOptions = new CreateLobbyOptions()
                    {
                        LocalUserId = localProductUserId,
                        MaxLobbyMembers = 2,
                        PermissionLevel = LobbyPermissionLevel.Publicadvertised,
                        PresenceEnabled = false,
                        AllowInvites = true,
                        BucketId = bucketId,
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

                    var createLobbyUtcs = new UniTaskCompletionSource<string>();

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

                    targetLobbyId = await createLobbyUtcs.Task;

                    // ロビー作成、または既存ロビー入室のどちらにも失敗している場合はここで終了
                    if (string.IsNullOrEmpty(targetLobbyId))
                    {
                        return string.Empty;
                    }

                    // === 新規作成時のみ：検索できるようにカスタム属性（部屋名）をロビーに紐付ける ===
                    Debug.Log($"ロビー [{targetLobbyId}] にカスタム属性（部屋名: {roomName}）を登録中...");

                    var updateLobbyModificationOptions = new UpdateLobbyModificationOptions()
                    {
                        LobbyId = targetLobbyId,
                        LocalUserId = localProductUserId
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

                            await updateLobbyUtcs.Task;
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

                // =================================================================
                // 【一体化処理】自身の表示名をメンバー属性としてロビーに登録
                // =================================================================
                if (!string.IsNullOrEmpty(targetLobbyId) && !string.IsNullOrEmpty(displayName))
                {
                    Debug.Log($"ロビー [{targetLobbyId}] に自身の表示名（{displayName}）を登録中...");

                    var updateOptions = new UpdateLobbyModificationOptions()
                    {
                        LobbyId = targetLobbyId,
                        LocalUserId = localProductUserId
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

                                bool isNameUpdateSuccess = await memberUpdateUtcs.Task;
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
                lobbySearchHandle.Release();
            }
        }

        /// <summary>
        /// 指定したロビーから退室します。
        /// </summary>
        /// <param name="localUserId">自身のProductUserId</param>
        /// <param name="lobbyId">退室するロビーID</param>
        /// <returns>成功時: true 失敗時: false</returns>
        public static async UniTask<bool> LeaveLobbyAsync(ProductUserId localUserId, string lobbyId)
        {
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

            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

            // 退室オプションの構築
            var leaveLobbyOptions = new LeaveLobbyOptions()
            {
                LocalUserId = localUserId,
                LobbyId = lobbyId
            };

            var utcs = new UniTaskCompletionSource<bool>();

            // EOSサーバーへ退室リクエストを送信
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

            return await utcs.Task;
        }

        /// <summary>
        /// ロビー内メンバー取得
        /// </summary>
        /// <param name="localUserId">自身のProductUserId</param>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <returns>ロビーメンバーのProductUserId配列</returns>
        public static List<ProductUserId> GetLobbyMembers(ProductUserId localUserId, string lobbyId)
        {
            // 0. 変数定義
            var list = new List<ProductUserId>();
            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

            // 1. ロビー詳細を取得
            // 【修正箇所】LocalUserId = localUserId を追加
            var copyOptions = new CopyLobbyDetailsHandleOptions()
            {
                LobbyId = lobbyId,
                LocalUserId = localUserId
            };

            Result result = lobbyInterface.CopyLobbyDetailsHandle(ref copyOptions, out LobbyDetails lobbyDetails);

            // 完全に取得失敗し、ハンドルも生成されていない場合のみ即リターン
            if (result != Result.Success || lobbyDetails == null)
            {
                Debug.LogError($"ロビー詳細の取得に失敗しました: {result}");
                return list;
            }

            // 2. ロビー内のメンバーを取得
            try
            {
                var countOptions = new LobbyDetailsGetMemberCountOptions();
                uint memberCount = lobbyDetails.GetMemberCount(ref countOptions);

                for (uint i = 0; i < memberCount; i++)
                {
                    var memberOptions = new LobbyDetailsGetMemberByIndexOptions() { MemberIndex = i };
                    ProductUserId memberPuid = lobbyDetails.GetMemberByIndex(ref memberOptions);
                    list.Add(memberPuid);
                }

                // Click.csの挙動（配列期待）に合わせて配列で返す
                return list;
            }
            finally
            {
                // ハンドル解放
                lobbyDetails.Release();
            }
        }


        /// <summary>
        /// ロビーのメンバー属性から全員の表示名を取得する
        /// </summary>
        /// <param name="members">メンバー一覧</param>
        /// <returns>表示名一覧</returns>
        public static Dictionary<ProductUserId, string> GetLobbyMemberDisplayNames(ProductUserId localUserId, string lobbyId)
        {
            var displayNamesMap = new Dictionary<ProductUserId, string>();
            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

            var copyOptions = new CopyLobbyDetailsHandleOptions()
            {
                LobbyId = lobbyId,
                LocalUserId = localUserId
            };

            if (lobbyInterface.CopyLobbyDetailsHandle(ref copyOptions, out LobbyDetails lobbyDetails) != Result.Success)
            {
                return displayNamesMap;
            }

            try
            {
                var members = GetLobbyMembers(localUserId, lobbyId);
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
                        // 【修正】DataがNullable型のため、Data.Value(構造体実体) からさらに .Value(AttributeDataValue) を経由して AsUtf8 を取得します
                        if (memberAttribute.Value.Data.HasValue)
                        {
                            displayNamesMap[memberPuid] = memberAttribute.Value.Data.Value.Value.AsUtf8;
                        }
                        else
                        {
                            displayNamesMap[memberPuid] = "Unknown Player";
                        }

                        // 【修正】構造体なので memberAttribute.Value.Release() は不要（削除）
                    }
                    else
                    {
                        displayNamesMap[memberPuid] = "Unknown Player";
                    }
                }
            }
            finally
            {
                // こちらのクラスハンドル（LobbyDetails）は確実に解放が必要です
                lobbyDetails.Release();
            }

            return displayNamesMap;
        }

        /// <summary>
        /// ロビーのホスト（オーナー）の ProductUserId を取得する
        /// </summary>
        /// <param name="localUserId">自身のProductUserId</param>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <returns>ホストのProductUserId（取得失敗時は null）</returns>
        public static ProductUserId GetLobbyHostPuid(ProductUserId localUserId, string lobbyId)
        {
            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

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

        /// <summary>
        /// ロビーメンバー変更通知登録
        /// </summary>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <param name="memberStatusNotificationId">通知ID</param>
        /// <param name="onMemberChangedCallback">通知コールバック</param>
        public static void RegisterLobbyNotifications(string lobbyId, ref ulong memberStatusNotificationId, Action onMemberChangedCallback)
        {
            var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();

            // すでに登録されている場合は一度解除
            UnregisterLobbyNotifications(memberStatusNotificationId);

            // 通知のオプション設定
            var memberStatusOptions = new AddNotifyLobbyMemberStatusReceivedOptions();

            // EOSサーバーに通知イベント（リスナー）を登録
            memberStatusNotificationId = lobbyInterface.AddNotifyLobbyMemberStatusReceived(
                ref memberStatusOptions,
                null,
                (ref LobbyMemberStatusReceivedCallbackInfo callbackInfo) =>
                {
                    // 自分が対象のロビーに対する通知か確認
                    if (callbackInfo.LobbyId != lobbyId) return;

                    // どんな変化が起きたかログを出す（デバッグ用）
                    Debug.Log($"ロビーメンバーに変化がありました。対象PUID: {callbackInfo.TargetUserId}, 状態: {callbackInfo.CurrentStatus}");

                    // callbackInfo.CurrentStatus の中身によって処理を分岐できます
                    // - LobbyMemberStatus.Joined : 誰かが入ってきた
                    // - LobbyMemberStatus.Left   : 誰かが出ていった
                    // - LobbyMemberStatus.Disconnected : 誰かの回線が切れた

                    // 変化があったので、UI（画面表示）を更新するためのコールバック関数を実行
                    onMemberChangedCallback?.Invoke();
                }
            );
        }

        /// <summary>
        /// ロビー通知登録を解除
        /// </summary>
        /// <param name="memberStatusNotificationId">通知ID</param>
        public static void UnregisterLobbyNotifications(ulong memberStatusNotificationId = 0)
        {
            if (memberStatusNotificationId != 0)
            {
                var lobbyInterface = EOSManager.Instance.GetEOSPlatformInterface().GetLobbyInterface();
                lobbyInterface.RemoveNotifyLobbyMemberStatusReceived(memberStatusNotificationId);
                memberStatusNotificationId = 0;
                Debug.Log("ロビーメンバー変更通知を解除しました。");
            }
        }

    }
}