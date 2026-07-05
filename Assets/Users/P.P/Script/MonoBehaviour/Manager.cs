using Epic.OnlineServices;
using UnityEngine;
using OriginalNameSpace.EOSMethod.P2P;
public class EOSNetworkManager : MonoBehaviour
{
    public static EOSNetworkManager Instance { get; private set; }

    [SerializeField] private string socketName = "MyGameDefaultSocket"; // ソケット名

    private ulong _notificationId = 0;
    private bool _isInitialized = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void InitializeP2P()
    {
        if (_isInitialized) return;

        // static クラスのメソッドを直接呼び出す
        _notificationId = EOSP2PMethod.StartListening(socketName);

        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        // 毎フレームの受信チェックも直接呼び出し
        EOSP2PMethod.UpdateReceiveLoop(OnMessageReceived);
    }

    public void SendChatMessage(ProductUserId targetUser, string message)
    {
        if (!_isInitialized) return;

        NetworkPacket packet = new NetworkPacket(message);
        byte[] data = packet.ToBytes();

        // 静的メソッドで送信
        EOSP2PMethod.SendPacketData(socketName, targetUser, data);
    }

    private void OnMessageReceived(ProductUserId fromUser, string incomingSocketName, byte[] rawData)
    {
        if (incomingSocketName != socketName) return;

        NetworkPacket receivedPacket = NetworkPacket.FromBytes(rawData);
        Debug.Log($"[受信] From: {fromUser} | Msg: {receivedPacket.Message}");
    }

    private void OnDestroy()
    {
        if (_notificationId != 0)
        {
            // 静的メソッドで待ち受け停止
            EOSP2PMethod.StopListening(_notificationId);
        }
    }
}