using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OriginalNameSpace.EOSMethod.P2P
{
    /// <summary>
    /// Unity標準型（Color, Vector3, Quaternion 等）に多い
    /// 「setterを持たない計算プロパティ（例: Color.linear, Vector3.normalized）」を
    /// シリアライズ対象から除外する共通ContractResolver。
    ///
    /// これらの計算プロパティは「自分自身と同じ型の新しいインスタンス」を返すことが多く、
    /// Newtonsoft.Jsonのデフォルト挙動（publicプロパティも辿る）だと
    /// 自己参照ループ(Self referencing loop detected)の原因になる。
    ///
    /// 一方、通常のデータクラス／構造体で使う「public フィールド」や
    /// 「setter付きの自動実装プロパティ（{ get; set; }）」はこれまで通り対象に含める。
    ///
    /// 個別の型ごとに JsonConverter を書かなくても、この Resolver を _jsonSettings に
    /// 1つ設定しておけば、同様のパターンの型が今後増えても自動的に対応できる。
    /// </summary>
    public class SelfReferenceSafeContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (member.MemberType == MemberTypes.Property)
            {
                var propertyInfo = (PropertyInfo)member;

                // setterが無い(=読み取り専用の計算プロパティ)は対象外にする
                if (!propertyInfo.CanWrite)
                {
                    property.ShouldSerialize = _ => false;
                    property.ShouldDeserialize = _ => false;
                }
            }

            return property;
        }
    }
}
