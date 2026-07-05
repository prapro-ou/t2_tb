using System;
using System.Text;

// UnityのJsonUtilityでシリアライズ（データ変換）可能にするための属性
[Serializable]
public struct NetworkPacket
{
    public string Message;   // チャットなどの文字列メッセージ
    public long Timestamp;   // 送信された時間（ミリ秒）

    /// <summary>
    /// 新しく送信データ（パケット）を作成するためのコンストラクタ
    /// </summary>
    public NetworkPacket(string message)
    {
        Message = message;
        // 現在のUTC時間をミリ秒のタイムスタンプとして記録
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 【送信時に使用】この構造体のデータを、ネットワーク送信用のバイト配列(byte[])に変換します（シリアライズ）
    /// </summary>
    public byte[] ToBytes()
    {
        // 1. 構造体を一度JSON文字列に変換 (例: "{"Message":"こんにちは","Timestamp":1234567}")
        string json = UnityEngine.JsonUtility.ToJson(this);

        // 2. JSON文字列をUTF8基準のバイト配列(byte[])に変換して返す
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// 【受信時に使用】届いた生のバイト配列(byte[])を、C#で扱える構造体の形に復元します（デシリアライズ）
    /// </summary>
    public static NetworkPacket FromBytes(byte[] bytes)
    {
        // 1. バイト配列を文字列(JSON)に戻す
        string json = Encoding.UTF8.GetString(bytes);

        // 2. JSON文字列をNetworkPacket構造体に変換して返す
        return UnityEngine.JsonUtility.FromJson<NetworkPacket>(json);
    }
}