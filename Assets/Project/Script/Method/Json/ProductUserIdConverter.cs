using System;
using Newtonsoft.Json;
using Epic.OnlineServices;

namespace OriginalNameSpace.EOSMethod.P2P
{
    /// <summary>
    /// EOSの ProductUserId 型を Newtonsoft.Json で送受信可能にするためのコンバーター。
    /// ProductUserId は内部にネイティブハンドルを持つだけの型でJSON化に向かないため、
    /// EOS標準の ToString() / FromString() を経由して文字列としてやり取りする。
    /// Dictionary のキーとして使われた場合も自動的に文字列化される。
    /// </summary>
    public class ProductUserIdConverter : JsonConverter<ProductUserId>
    {
        public override void WriteJson(JsonWriter writer, ProductUserId value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }

        public override ProductUserId ReadJson(JsonReader reader, Type objectType, ProductUserId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            string str = reader.Value as string;
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            return ProductUserId.FromString(str);
        }
    }
}
