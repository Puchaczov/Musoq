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
    /// <returns>Converted boolean value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? FromBytesToBool(byte[] value)
    {
        if (value == null || value.Length < sizeof(bool))
            return null;

        return BitConverter.ToBoolean(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a character.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted character, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? FromBytesToChar(byte[] value)
    {
        if (value == null || value.Length < sizeof(char))
            return null;

        return BitConverter.ToChar(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 16-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 16-bit signed integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? FromBytesToInt16(byte[] value)
    {
        if (value == null || value.Length < sizeof(short))
            return null;

        return BitConverter.ToInt16(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 16-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 16-bit unsigned integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? FromBytesToUInt16(byte[] value)
    {
        if (value == null || value.Length < sizeof(ushort))
            return null;

        return BitConverter.ToUInt16(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 32-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 32-bit signed integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? FromBytesToInt32(byte[] value)
    {
        if (value == null || value.Length < sizeof(int))
            return null;

        return BitConverter.ToInt32(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 32-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 32-bit unsigned integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? FromBytesToUInt32(byte[] value)
    {
        if (value == null || value.Length < sizeof(uint))
            return null;

        return BitConverter.ToUInt32(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 64-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 64-bit signed integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromBytesToInt64(byte[] value)
    {
        if (value == null || value.Length < sizeof(long))
            return null;

        return BitConverter.ToInt64(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 64-bit unsigned integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? FromBytesToUInt64(byte[] value)
    {
        if (value == null || value.Length < sizeof(ulong))
            return null;

        return BitConverter.ToUInt64(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a half-precision floating point value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted half-precision floating point value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public Half? FromBytesToHalf(byte[] value)
    {
        const int halfSize = 2; // Half is 2 bytes (16-bit)
        if (value == null || value.Length < halfSize)
            return null;

        return BitConverter.ToHalf(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a float value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>The float value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? FromBytesToFloat(byte[] value)
    {
        if (value == null || value.Length < sizeof(float))
            return null;

        return BitConverter.ToSingle(value, 0);
    }

    /// <summary>
    ///     Converts bytes to a double value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>The double value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? FromBytesToDouble(byte[] value)
    {
        if (value == null || value.Length < sizeof(double))
            return null;

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
        if (value == null || value.Length == 0)
            return string.Empty;

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
