using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {

        [BindableMethod]
        public string ToHex(byte[] bytes, string delimiter = "")
        {
            var hexBuilder = new StringBuilder();

            if (bytes.Length > 0)
            {
                for (int i = 0; i < bytes.Length - 1; i++)
                {
                    var byteValue = bytes[i];
                    hexBuilder.Append(byteValue.ToString("X2"));
                    hexBuilder.Append(delimiter);
                }

                hexBuilder.Append(bytes[bytes.Length - 1].ToString("X2"));
            }

            return hexBuilder.ToString();
        }

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        [BindableMethod]
        public string ToHex<T>(T value) where T : IConvertible
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return ToHex(GetBytes(value.ToBoolean(InvariantCulture)));
                case TypeCode.Byte:
                    return ToHex(GetBytes(value.ToByte(InvariantCulture)));
                case TypeCode.Char:
                    return ToHex(GetBytes(value.ToChar(InvariantCulture)));
                case TypeCode.DateTime:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.DBNull:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Decimal:
                    return ToHex(GetBytes(value.ToDecimal(InvariantCulture)));
                case TypeCode.Double:
                    return ToHex(GetBytes(value.ToDouble(InvariantCulture)));
                case TypeCode.Empty:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Int16:
                    return ToHex(GetBytes(value.ToInt16(InvariantCulture)));
                case TypeCode.Int32:
                    return ToHex(GetBytes(value.ToInt32(InvariantCulture)));
                case TypeCode.Int64:
                    return ToHex(GetBytes(value.ToInt64(InvariantCulture)));
                case TypeCode.Object:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.SByte:
                    return ToHex(GetBytes(value.ToSByte(InvariantCulture)));
                case TypeCode.Single:
                    return ToHex(GetBytes(value.ToSingle(InvariantCulture)));
                case TypeCode.String:
                    return ToHex(GetBytes(value.ToString(InvariantCulture)));
                case TypeCode.UInt16:
                    return ToHex(GetBytes(value.ToUInt16(InvariantCulture)));
                case TypeCode.UInt32:
                    return ToHex(GetBytes(value.ToUInt32(InvariantCulture)));
                case TypeCode.UInt64:
                    return ToHex(GetBytes(value.ToUInt64(InvariantCulture)));
            }

            return "CONVERSION_NOT_SUPPORTED";
        }

        [BindableMethod]
        public string ToBin<T>(T value) where T : IConvertible
        {
            return ToBase(value, 2);
        }

        [BindableMethod]
        public string ToOcta<T>(T value) where T : IConvertible
        {
            return ToBase(value, 8);
        }

        [BindableMethod]
        public string ToDec<T>(T value) where T : IConvertible
        {
            return ToBase(value, 10);
        }

        [BindableMethod]
        public decimal? ToDecimal(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return null;
        }

        [BindableMethod]
        public decimal? ToDecimal(string value, string culture)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo(culture), out decimal result))
                return result;

            return null;
        }

        [BindableMethod]
        public decimal? ToDecimal(long? value)
        {
            return value;
        }

        [BindableMethod]
        public long? ToLong(string value)
        {
            if (long.TryParse(value, out var number))
                return number;

            return null;
        }

        [BindableMethod]
        public string ToString(DateTimeOffset? date)
        {
            return date?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(decimal? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(long? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        [BindableMethod]
        public string ToString(object obj)
        {
            return obj?.ToString();
        }

        [BindableMethod]
        public string ToBase64(byte[] array)
        {
            return Convert.ToBase64String(array, Base64FormattingOptions.None);
        }

        [BindableMethod]
        public string ToBase64(byte[] array, int offset, int length)
        {
            return Convert.ToBase64String(array, offset, length, Base64FormattingOptions.None);
        }

        [BindableMethod]
        public byte[] FromBase64(string base64String)
        {
            return Convert.FromBase64String(base64String);
        }

        private string ToBase<T>(T value, int baseNumber) where T : IConvertible
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return Convert.ToString(value.ToBoolean(InvariantCulture) ? 1 : 0, baseNumber);
                case TypeCode.Byte:
                    return Convert.ToString(value.ToByte(InvariantCulture), baseNumber);
                case TypeCode.Char:
                    return Convert.ToString(value.ToChar(InvariantCulture), baseNumber);
                case TypeCode.DateTime:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.DBNull:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Decimal:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Double:
                    return Convert.ToString(BitConverter.DoubleToInt64Bits(value.ToDouble(InvariantCulture)), baseNumber);
                case TypeCode.Empty:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Int16:
                    return Convert.ToString(value.ToInt16(InvariantCulture), baseNumber);
                case TypeCode.Int32:
                    return Convert.ToString(value.ToInt32(InvariantCulture), baseNumber);
                case TypeCode.Int64:
                    return Convert.ToString(value.ToInt64(InvariantCulture), baseNumber);
                case TypeCode.Object:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.SByte:
                    return Convert.ToString(value.ToSByte(InvariantCulture), baseNumber);
                case TypeCode.Single:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.String:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.UInt16:
                    return Convert.ToString(value.ToUInt16(InvariantCulture), baseNumber);
                case TypeCode.UInt32:
                    return Convert.ToString(value.ToUInt32(InvariantCulture), baseNumber);
                case TypeCode.UInt64:
                    return "CONVERSION_NOT_SUPPORTED";
            }

            return "CONVERSION_NOT_SUPPORTED";
        }
    }
}
