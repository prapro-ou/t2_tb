using System;
using System.ComponentModel;
using System.Globalization;
using Epic.OnlineServices;

namespace OriginalNameSpace.EOSMethod.P2P
{
    /// <summary>
    /// Newtonsoft.Json が Dictionary&lt;ProductUserId, TValue&gt; のキーを
    /// 文字列⇔ProductUserId で相互変換するために使用する TypeConverter。
    ///
    /// Newtonsoft.Json は Dictionary のキー型が string / プリミティブ / enum 以外の場合、
    /// 値のシリアライズに使う JsonConverter(ProductUserIdConverter) ではなく、
    /// System.ComponentModel.TypeConverter 経由でキーの文字列⇔型変換を行う仕様のため、
    /// これを別途用意する必要がある。
    ///
    /// ProductUserId は EOS SDK 提供のクラスでソースを修正できないため、
    /// この TypeConverter を実行時に TypeDescriptor.AddAttributes で紐付ける
    /// （EOSP2PMethod の静的コンストラクタ参照）。
    /// </summary>
    public class ProductUserIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str)
            {
                return ProductUserId.FromString(str);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is ProductUserId puid)
            {
                return puid.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
