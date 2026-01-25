using System;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts bytes to boolean value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted boolean value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool FromBytesToBool(byte[] value)
    {
        return BitConverter.ToBoolean(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 16-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 16-bit signed integer.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short FromBytesToInt16(byte[] value)
    {
        return BitConverter.ToInt16(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 16-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 16-bit unsigned integer.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort FromBytesToUInt16(byte[] value)
    {
        return BitConverter.ToUInt16(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 32-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 32-bit signed integer.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int FromBytesToInt32(byte[] value)
    {
        return BitConverter.ToInt32(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 32-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 32-bit unsigned integer.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint FromBytesToUInt32(byte[] value)
    {
        return BitConverter.ToUInt32(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 64-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 64-bit signed integer.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long FromBytesToInt64(byte[] value)
    {
        return BitConverter.ToInt64(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 64-bit signed integer.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong FromBytesToUInt64(byte[] value)
    {
        return BitConverter.ToUInt64(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a float value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>The float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float FromBytesToFloat(byte[] value)
    {
        return BitConverter.ToSingle(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a float value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>The float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double FromBytesToDouble(byte[] value)
    {
        return BitConverter.ToDouble(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a string.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>The string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string FromBytesToString(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }

    /// <summary>
    ///     Converts bytes to a string using the specified encoding.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="encodingName">The encoding to use (e.g., "utf-8", "ascii", "utf-16le", "utf-16be", "latin1").</param>
    /// <returns>The string value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string ToText(byte[] value, string encodingName)
    {
        if (value == null || value.Length == 0)
            return string.Empty;

        var encoding = encodingName?.ToLowerInvariant() switch
        {
            "utf-8" or "utf8" => Encoding.UTF8,
            "utf-16" or "utf16" or "unicode" => Encoding.Unicode,
            "utf-16le" or "utf16le" => Encoding.Unicode,
            "utf-16be" or "utf16be" => Encoding.BigEndianUnicode,
            "ascii" => Encoding.ASCII,
            "latin1" or "iso-8859-1" => Encoding.Latin1,
            _ => Encoding.UTF8
        };

        return encoding.GetString(value);
    }
}
