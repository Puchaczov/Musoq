using System;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Converts given bytes to hex with defined delimiter
    /// </summary>
    /// <param name="bytes">The bytes</param>
    /// <param name="delimiter">The delimiter</param>
    /// <returns>Hex representation of a given bytes</returns>
    [BindableMethod]
    public string? ToHex(byte[]? bytes, string delimiter = "")
    {
        if (bytes == null)
            return null;

        var hexBuilder = new StringBuilder();

        if (bytes.Length <= 0) return hexBuilder.ToString();
            
        for (var i = 0; i < bytes.Length - 1; i++)
        {
            var byteValue = bytes[i];
            hexBuilder.Append(byteValue.ToString("X2"));
            hexBuilder.Append(delimiter);
        }

        hexBuilder.Append(bytes[^1].ToString("X2"));

        return hexBuilder.ToString();
    }

        /// <summary>
        /// Converts given value to binary
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Hex representation of a given bytes</returns>
        [BindableMethod]
        public string? ToHex<T>(T value) where T : IConvertible
        {
            return Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Boolean => ToHex(GetBytes(value.ToBoolean(CultureInfo.CurrentCulture))),
                TypeCode.Byte => ToHex(GetBytes(value.ToByte(CultureInfo.CurrentCulture))),
                TypeCode.Char => ToHex(GetBytes(value.ToChar(CultureInfo.CurrentCulture))),
                TypeCode.DateTime => "CONVERSION_NOT_SUPPORTED",
                TypeCode.DBNull => "CONVERSION_NOT_SUPPORTED",
                TypeCode.Decimal => ToHex(GetBytes(value.ToDecimal(CultureInfo.CurrentCulture))),
                TypeCode.Double => ToHex(GetBytes(value.ToDouble(CultureInfo.CurrentCulture))),
                TypeCode.Empty => "CONVERSION_NOT_SUPPORTED",
                TypeCode.Int16 => ToHex(GetBytes(value.ToInt16(CultureInfo.CurrentCulture))),
                TypeCode.Int32 => ToHex(GetBytes(value.ToInt32(CultureInfo.CurrentCulture))),
                TypeCode.Int64 => ToHex(GetBytes(value.ToInt64(CultureInfo.CurrentCulture))),
                TypeCode.Object => "CONVERSION_NOT_SUPPORTED",
                TypeCode.SByte => ToHex(GetBytes(value.ToSByte(CultureInfo.CurrentCulture))),
                TypeCode.Single => ToHex(GetBytes(value.ToSingle(CultureInfo.CurrentCulture))),
                TypeCode.String => ToHex(GetBytes(value.ToString(CultureInfo.CurrentCulture))),
                TypeCode.UInt16 => ToHex(GetBytes(value.ToUInt16(CultureInfo.CurrentCulture))),
                TypeCode.UInt32 => ToHex(GetBytes(value.ToUInt32(CultureInfo.CurrentCulture))),
                TypeCode.UInt64 => ToHex(GetBytes(value.ToUInt64(CultureInfo.CurrentCulture))),
                _ => "CONVERSION_NOT_SUPPORTED"
            };
        }
}