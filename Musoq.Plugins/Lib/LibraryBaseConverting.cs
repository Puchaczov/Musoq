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
    [MethodCategory(MethodCategories.Conversion)]
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
    [MethodCategory(MethodCategories.Conversion)]
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
    [MethodCategory(MethodCategories.Conversion)]
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
    [MethodCategory(MethodCategories.Conversion)]
    public string ToDec<T>(T value) where T : IConvertible
    {
        return ToBase(value, 10);
    }
        
    /// <summary>
    /// Converts given bytes to base64 string
    /// </summary>
    /// <param name="value">The bytes to encode</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(byte[]? value)
    {
        if (value == null)
            return null;
            
        return Convert.ToBase64String(value, Base64FormattingOptions.None);
    }

    /// <summary>
    /// Converts given array of bytes to base64 string with offset and length
    /// </summary>
    /// <param name="value">The bytes to encode</param>
    /// <param name="offset">The offset of bytes</param>
    /// <param name="length">The length of bytes</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(byte[]? value, int offset, int length)
    {
        if (value == null)
            return null;
            
        return Convert.ToBase64String(value, offset, length, Base64FormattingOptions.None);
    }

    /// <summary>
    /// Converts given string to base64 encoded string using UTF-8 encoding
    /// </summary>
    /// <param name="value">The string to encode</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(string? value)
    {
        if (value == null)
            return null;
            
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value), Base64FormattingOptions.None);
    }

    /// <summary>
    /// Converts given string to base64 encoded string using specified encoding
    /// </summary>
    /// <param name="value">The string to encode</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(string? value, string encodingName)
    {
        if (value == null)
            return null;
            
        var encoding = Encoding.GetEncoding(encodingName);
        return Convert.ToBase64String(encoding.GetBytes(value), Base64FormattingOptions.None);
    }

    /// <summary>
    /// Converts base64 encoded string to bytes array
    /// </summary>
    /// <param name="value">The base64 encoded string</param>
    /// <returns>Decoded bytes, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? FromBase64(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
            
        return Convert.FromBase64String(value);
    }

    /// <summary>
    /// Converts base64 encoded string directly to a decoded string using UTF-8 encoding
    /// </summary>
    /// <param name="value">The base64 encoded string</param>
    /// <returns>Decoded string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? FromBase64ToString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
            
        var bytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Converts base64 encoded string directly to a decoded string using specified encoding
    /// </summary>
    /// <param name="value">The base64 encoded string</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>Decoded string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? FromBase64ToString(string? value, string encodingName)
    {
        if (string.IsNullOrEmpty(value))
            return null;
            
        var encoding = Encoding.GetEncoding(encodingName);
        var bytes = Convert.FromBase64String(value);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Converts hexadecimal string to long integer
    /// </summary>
    /// <param name="value">The hexadecimal string (with or without 0x prefix)</param>
    /// <returns>Converted long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleanValue = value.Trim();
        
        
        if (cleanValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            cleanValue = cleanValue.Substring(2);

        try
        {
            return Convert.ToInt64(cleanValue, 16);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts binary string to long integer
    /// </summary>
    /// <param name="value">The binary string (with or without 0b prefix)</param>
    /// <returns>Converted long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromBin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleanValue = value.Trim();
        
        
        if (cleanValue.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            cleanValue = cleanValue.Substring(2);

        try
        {
            return Convert.ToInt64(cleanValue, 2);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts octal string to long integer
    /// </summary>
    /// <param name="value">The octal string (with or without 0o prefix)</param>
    /// <returns>Converted long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromOct(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleanValue = value.Trim();
        
        
        if (cleanValue.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            cleanValue = cleanValue.Substring(2);

        try
        {
            return Convert.ToInt64(cleanValue, 8);
        }
        catch
        {
            return null;
        }
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