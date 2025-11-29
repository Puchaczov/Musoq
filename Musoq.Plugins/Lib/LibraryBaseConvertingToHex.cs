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
    [MethodCategory(MethodCategories.Conversion)]
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
    [MethodCategory(MethodCategories.Conversion)]
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

    /// <summary>
    /// Converts a hex string to bytes array.
    /// Supports hex strings with or without delimiters (spaces, dashes, colons).
    /// </summary>
    /// <param name="hexString">The hex string (e.g., "48656C6C6F" or "48 65 6C 6C 6F" or "48-65-6C-6C-6F")</param>
    /// <returns>The decoded bytes, or null if input is null or invalid</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? FromHexToBytes(string? hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return null;

        // Remove common delimiters
        var cleanHex = hexString
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(":", "")
            .Replace("0x", "")
            .Replace("0X", "");

        if (cleanHex.Length % 2 != 0)
            return null; // Invalid hex string - must have even number of characters

        try
        {
            var bytes = new byte[cleanHex.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(cleanHex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a hex string directly to a UTF-8 string.
    /// </summary>
    /// <param name="hexString">The hex string representing UTF-8 encoded text</param>
    /// <returns>The decoded string, or null if input is null or invalid</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? FromHexToString(string? hexString)
    {
        var bytes = FromHexToBytes(hexString);
        return bytes == null ? null : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Converts a hex string to a string using the specified encoding.
    /// </summary>
    /// <param name="hexString">The hex string</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decoded string, or null if input is null or invalid</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? FromHexToString(string? hexString, string encodingName)
    {
        var bytes = FromHexToBytes(hexString);
        if (bytes == null)
            return null;

        var encoding = Encoding.GetEncoding(encodingName);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Converts a string to its hex representation using UTF-8 encoding.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>Hex representation of the string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToHexFromString(string? value)
    {
        if (value == null)
            return null;

        return ToHex(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Converts a string to its hex representation using the specified encoding.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>Hex representation of the string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToHexFromString(string? value, string encodingName)
    {
        if (value == null)
            return null;

        var encoding = Encoding.GetEncoding(encodingName);
        return ToHex(encoding.GetBytes(value));
    }
}