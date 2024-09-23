using System;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Converts given bytes to binary with defined delimiter
    /// </summary>
    /// <param name="bytes">The bytes</param>
    /// <param name="delimiter">The delimiter</param>
    /// <returns>Binary representation of a given bytes</returns>
    [BindableMethod]
    public string? ToBin(byte[]? bytes, string delimiter = "")
    {
        if (bytes == null)
            return null;

        var builder = new StringBuilder();

        foreach (var @byte in bytes)
        {
            var binaryRepresentation = Convert
                .ToString(@byte, 2)
                .PadLeft(8, '0');

            builder.Append(binaryRepresentation);
            builder.Append(delimiter);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts given value to binary
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Binary representation of a given bytes</returns>
    [BindableMethod]
    public string ToBin<T>(T value) where T : IConvertible
    {
        return ToBase(value, 2);
    }

    /// <summary>
    /// Converts given value to octal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Hex representation of a given bytes</returns>
    [BindableMethod]
    public string ToOcta<T>(T value) where T : IConvertible
    {
        return ToBase(value, 8);
    }

    /// <summary>
    /// Converts given value to decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Decimal representation of a given value</returns>
    [BindableMethod]
    public string ToDec<T>(T value) where T : IConvertible
    {
        return ToBase(value, 10);
    }
        
    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string ToBase64(byte[] value)
    {
        return Convert.ToBase64String(value, Base64FormattingOptions.None);
    }

    /// <summary>
    /// Converts given array of bytes to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="offset">The offset of bytes</param>
    /// <param name="length">The length of bytes</param>
    /// <returns>Converted to base64 value</returns>
    [BindableMethod]
    public string ToBase64(byte[] value, int offset, int length)
    {
        return Convert.ToBase64String(value, offset, length, Base64FormattingOptions.None);
    }

    /// <summary>
    /// Converts given string to bytes array
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to base64 value</returns>
    [BindableMethod]
    public byte[] FromBase64(string value)
    {
        return Convert.FromBase64String(value);
    }

    private string ToBase<T>(T value, int baseNumber) where T : IConvertible
    {
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Boolean => Convert.ToString(value.ToBoolean(CultureInfo.CurrentCulture) ? 1 : 0, baseNumber),
            TypeCode.Byte => Convert.ToString(value.ToByte(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Char => Convert.ToString(value.ToChar(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.DateTime => "CONVERSION_NOT_SUPPORTED",
            TypeCode.DBNull => "CONVERSION_NOT_SUPPORTED",
            TypeCode.Decimal => "CONVERSION_NOT_SUPPORTED",
            TypeCode.Double => Convert.ToString(
                BitConverter.DoubleToInt64Bits(value.ToDouble(CultureInfo.CurrentCulture)), baseNumber),
            TypeCode.Empty => "CONVERSION_NOT_SUPPORTED",
            TypeCode.Int16 => Convert.ToString(value.ToInt16(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Int32 => Convert.ToString(value.ToInt32(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Int64 => Convert.ToString(value.ToInt64(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Object => "CONVERSION_NOT_SUPPORTED",
            TypeCode.SByte => Convert.ToString(value.ToSByte(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Single => "CONVERSION_NOT_SUPPORTED",
            TypeCode.String => "CONVERSION_NOT_SUPPORTED",
            TypeCode.UInt16 => Convert.ToString(value.ToUInt16(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.UInt32 => Convert.ToString(value.ToUInt32(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.UInt64 => "CONVERSION_NOT_SUPPORTED",
            _ => "CONVERSION_NOT_SUPPORTED"
        };
    }
}