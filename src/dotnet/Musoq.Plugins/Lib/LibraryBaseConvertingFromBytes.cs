using System;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static byte[] PadBytes(byte[] value, int requiredSize)
    {
        if (value.Length >= requiredSize)
            return value;

        var padded = new byte[requiredSize];
        Array.Copy(value, 0, padded, 0, value.Length);

        return padded;
    }

    private static T? FromBytesCore<T>(byte[] value, bool padIfNeeded, int requiredSize, Func<byte[], int, T> converter)
        where T : struct
    {
        if (value == null)
            return null;

        if (value.Length < requiredSize)
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, requiredSize);
        }

        return converter(value, 0);
    }

    /// <summary>
    ///     Converts bytes to boolean value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted boolean value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? FromBytesToBool(byte[] value) => FromBytesToBool(value, false);

    /// <summary>
    ///     Converts bytes to boolean value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted boolean value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? FromBytesToBool(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(bool), BitConverter.ToBoolean);

    /// <summary>
    ///     Converts bytes to a character.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted character, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? FromBytesToChar(byte[] value) => FromBytesToChar(value, false);

    /// <summary>
    ///     Converts bytes to a character.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted character, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? FromBytesToChar(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(char), BitConverter.ToChar);

    /// <summary>
    ///     Converts bytes to a 16-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 16-bit signed integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? FromBytesToInt16(byte[] value) => FromBytesToInt16(value, false);

    /// <summary>
    ///     Converts bytes to a 16-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 16-bit signed integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? FromBytesToInt16(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(short), BitConverter.ToInt16);

    /// <summary>
    ///     Converts bytes to a 16-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 16-bit unsigned integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? FromBytesToUInt16(byte[] value) => FromBytesToUInt16(value, false);

    /// <summary>
    ///     Converts bytes to a 16-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 16-bit unsigned integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? FromBytesToUInt16(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(ushort), BitConverter.ToUInt16);

    /// <summary>
    ///     Converts bytes to a 32-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 32-bit signed integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? FromBytesToInt32(byte[] value) => FromBytesToInt32(value, false);

    /// <summary>
    ///     Converts bytes to a 32-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 32-bit signed integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? FromBytesToInt32(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(int), BitConverter.ToInt32);

    /// <summary>
    ///     Converts bytes to a 32-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 32-bit unsigned integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? FromBytesToUInt32(byte[] value) => FromBytesToUInt32(value, false);

    /// <summary>
    ///     Converts bytes to a 32-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 32-bit unsigned integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? FromBytesToUInt32(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(uint), BitConverter.ToUInt32);

    /// <summary>
    ///     Converts bytes to a 64-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 64-bit signed integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromBytesToInt64(byte[] value) => FromBytesToInt64(value, false);

    /// <summary>
    ///     Converts bytes to a 64-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 64-bit signed integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromBytesToInt64(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(long), BitConverter.ToInt64);

    /// <summary>
    ///     Converts bytes to a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted 64-bit unsigned integer, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? FromBytesToUInt64(byte[] value) => FromBytesToUInt64(value, false);

    /// <summary>
    ///     Converts bytes to a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 64-bit unsigned integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? FromBytesToUInt64(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(ulong), BitConverter.ToUInt64);

    /// <summary>
    ///     Converts bytes to a half-precision floating point value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted half-precision floating point value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public Half? FromBytesToHalf(byte[] value) => FromBytesToHalf(value, false);

    /// <summary>
    ///     Converts bytes to a half-precision floating point value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted half-precision floating point value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public Half? FromBytesToHalf(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, 2, BitConverter.ToHalf);

    /// <summary>
    ///     Converts bytes to a float value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>The float value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? FromBytesToFloat(byte[] value) => FromBytesToFloat(value, false);

    /// <summary>
    ///     Converts bytes to a float value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>The float value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? FromBytesToFloat(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(float), BitConverter.ToSingle);

    /// <summary>
    ///     Converts bytes to a double value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>The double value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? FromBytesToDouble(byte[] value) => FromBytesToDouble(value, false);

    /// <summary>
    ///     Converts bytes to a double value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>The double value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? FromBytesToDouble(byte[] value, bool padIfNeeded) =>
        FromBytesCore(value, padIfNeeded, sizeof(double), BitConverter.ToDouble);

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
