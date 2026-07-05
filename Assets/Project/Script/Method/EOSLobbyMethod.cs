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
        private const string SearchKeyRoomName = "ROOM_NAME";

        /// <summary>
        /// 端末の「デバイスID」を使用して、EOS Connect サービスへのログインを非同期で実行します。
        /// 成功時はプレイヤーの識別子（ProductUserId）を返し、失敗時は null を返します。
        /// </summary>
        public static async UniTask<ProductUserId> LoginAsync(string userName)
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
                    },
                    UserLoginInfo = new UserLoginInfo
                    {
                        DisplayName = userName
                    }
                };
                Debug.Log(loginOptions.UserLoginInfo);
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
                Debug.LogError($"デバイスIDの作成・取得に失敗しました: {deviceIdResult.ResultCode}");
                return null;
            }
        }

        /// <summary>
        /// 指定した名前のロビーを検索
        /// 存在する場合:自動入室
        /// 存在しない場合:その部屋名で新規ロビーを作成
        /// 入室または作成されたロビーのID（失敗時は空文字）を返す
        /// </summary>
        /// <param name="roomName">合流・作成ロビーの部屋名</param>
        /// <returns>成功時:ロビーID 失敗時:空文字</returns>
        public static async UniTask<string> JoinOrCreateGameLobbyAsync(string roomName)
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

            try
            {
                // バケットIDの設定（同じ仕組みのロビーが所属する大枠の空間）
                string bucketId = $"PRIVATE_ROOM";

                // 検索フィルターにカスタム属性を指定
                var attributeFilterOptions = new LobbySearchSetParameterOptions()
                {
                    Parameter = new AttributeData()
                    {
                        Key = SearchKeyRoomName,
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

                                return await joinUtcs.Task;
                            }
                            finally
                            {
                                foundLobbyDetails.Release();
                            }
                        }
                        else
                        {
                            Debug.LogError($"ロビー詳細ハンドルのコピーに失敗しました。 エラーコード: {copyResult}");
                            return string.Empty;
                        }
                    }
                }

                // 3.2.検索結果が存在しない場合、新規作成
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

                string createdLobbyId = await createLobbyUtcs.Task;

                // ロビー作成に失敗した場合は終了
                if (string.IsNullOrEmpty(createdLobbyId))
                {
                    return string.Empty;
                }

                // =================================================================
                // 【追加実装】作成成功後、検索できるようにカスタム属性（部屋名）をロビーに紐付ける
                // =================================================================
                Debug.Log($"ロビー [{createdLobbyId}] にカスタム属性（部屋名: {roomName}）を登録中...");

                var updateLobbyModificationOptions = new UpdateLobbyModificationOptions()
                {
                    LobbyId = createdLobbyId,
                    LocalUserId = localProductUserId
                };

                // 1. ロビー修正用ハンドルの取得
                Result modificationResult = lobbyInterface.UpdateLobbyModification(ref updateLobbyModificationOptions, out LobbyModification lobbyModificationHandle);

                if (modificationResult == Result.Success && lobbyModificationHandle != null)
                {
                    try
                    {
                        // 2. 属性データの構築
                        var attributeData = new AttributeData()
                        {
                            Key = SearchKeyRoomName,
                            Value = roomName
                        };

                        var lobbyModificationAddAttributeOptions = new LobbyModificationAddAttributeOptions()
                        {
                            Attribute = attributeData,
                            // 重要: 他のユーザーが検索（Find）で見つけられるように「Publicadvertised」を指定
                            Visibility = LobbyAttributeVisibility.Public
                        };

                        // 3. ハンドルに対して属性を設定
                        lobbyModificationHandle.AddAttribute(ref lobbyModificationAddAttributeOptions);

                        // 4. 設定した内容をEOSサーバーへ反映（アップデートリクエスト）
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

                        // アップデート完了まで待機
                        await updateLobbyUtcs.Task;
                    }
                    finally
                    {
                        // 使い終わった修正用ハンドルを確実に解放
                        lobbyModificationHandle.Release();
                    }
                }
                else
                {
                    Debug.LogError($"LobbyModificationハンドルの取得に失敗しました: {modificationResult}");
                }

                return createdLobbyId;
            }
            finally
            {
                lobbySearchHandle.Release();
            }
        }

        /// <summary>
        /// ロビー内のメンバーを取得
        /// </summary>
        /// <param name="localUserId">自身のProductUserId</param>
        /// <param name="lobbyId">対象のロビーID</param>
        /// <returns>メンバーのProductUserId配列</returns>
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
        /// ロビーのメンバー変更通知を登録します。
        /// </summary>
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
        /// ゲームを終了する場合や、ロビーを退室する際に通知登録を解除します（メモリリーク防止）。
        /// </summary>
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

        /// <summary>
        /// ロビーメンバー全員の表示名を一括で取得する
        /// </summary>
        /// <param name="localProductUserId">あなた（自分自身）の PUID</param>
        /// <param name="lobbyMembers">自分を含めた、ロビー内にいる全員の PUID 配列</param>
        /// <returns>PUID をキー、表示名を値とした辞書</returns>
        public static async UniTask<Dictionary<ProductUserId, string>> FetchLobbyDisplayNamesAsync(ProductUserId localProductUserId, List<ProductUserId> lobbyMembers)
        {
            var displayNamesMap = new Dictionary<ProductUserId, string>();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
            {
                return displayNamesMap;
            }

            var connectInterface = EOSManager.Instance.GetEOSConnectInterface();
            if (connectInterface == null)
            {
                Debug.LogError("Connect Interface の取得に失敗しました。");
                return displayNamesMap;
            }

            // 1. サーバーへ一括問い合わせ（クエリ）を行うための設定
            // 引数で受け取った配列をそのまま渡すことができます
            var queryOptions = new QueryProductUserIdMappingsOptions()
            {
                LocalUserId = localProductUserId,
                ProductUserIds = lobbyMembers.ToArray()
            };

            var utcs = new UniTaskCompletionSource<Result>();

            // 2. サーバーに問い合わせを投げる（非同期）
            connectInterface.QueryProductUserIdMappings(ref queryOptions, null, (ref QueryProductUserIdMappingsCallbackInfo callbackInfo) =>
            {
                utcs.TrySetResult(callbackInfo.ResultCode);
            });

            // 3. サーバーからの返答を待つ
            Result queryResult = await utcs.Task;
            if (queryResult != Result.Success)
            {
                Debug.LogError($"ロビーメンバーの情報クエリに失敗しました: {queryResult}");
                return displayNamesMap; // 失敗時は空の辞書を返す
            }
            // 4. クエリ成功後、ローカルキャッシュから1人ずつ名前を抽出する
            foreach (var targetUserId in lobbyMembers)
            {
                // 該当ユーザーに紐づいている外部アカウント（デバイスIDやSteam等）の総数を取得
                var getCountOptions = new GetProductUserExternalAccountCountOptions()
                {
                    TargetUserId = targetUserId
                };
                uint accountCount = connectInterface.GetProductUserExternalAccountCount(ref getCountOptions);

                string foundDisplayName = "Unknown Player"; // 名前が見つからなかった時のフォールバック

                // アカウントをループして DisplayName を探す
                for (uint i = 0; i < accountCount; i++)
                {
                    // 正しいオプション型名：CopyProductUserExternalAccountByIndexOptions
                    var copyOptions = new CopyProductUserExternalAccountByIndexOptions()
                    {
                        TargetUserId = targetUserId,
                        ExternalAccountInfoIndex = i // インデックスを指定
                    };

                    // 正しい関数名：CopyProductUserExternalAccountByIndex
                    Result copyResult = connectInterface.CopyProductUserExternalAccountByIndex(ref copyOptions, out ExternalAccountInfo? externalAccountInfo);

                    if (copyResult == Result.Success && externalAccountInfo != null)
                    {
                        if (!string.IsNullOrEmpty(externalAccountInfo.Value.DisplayName))
                        {
                            foundDisplayName = externalAccountInfo.Value.DisplayName;
                            break; // 名前が見つかったらこの人のループは抜ける
                        }
                    }
                }

                // 辞書に PUID と名前のペアを追加
                displayNamesMap[targetUserId] = foundDisplayName;
            }
            return displayNamesMap;
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

    }
}