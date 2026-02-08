using System;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
        private static byte[]? PadBytes(byte[] value, int requiredSize)
    {
        if (value == null)
            return null;

        if (value.Length >= requiredSize)
            return value;

        var padded = new byte[requiredSize];
        Array.Copy(value, 0, padded, 0, value.Length);
        
        return padded;
    }

    /// <summary>
    ///     Converts bytes to boolean value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <returns>Converted boolean value, or null if insufficient bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? FromBytesToBool(byte[] value)
    {
        return FromBytesToBool(value, false);
    }

    /// <summary>
    ///     Converts bytes to boolean value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted boolean value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? FromBytesToBool(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(bool))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(bool));
            if (value == null)
                return null;
        }

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
        return FromBytesToChar(value, false);
    }

    /// <summary>
    ///     Converts bytes to a character.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted character, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? FromBytesToChar(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(char))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(char));
            if (value == null)
                return null;
        }

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
        return FromBytesToInt16(value, false);
    }

    /// <summary>
    ///     Converts bytes to a 16-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 16-bit signed integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? FromBytesToInt16(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(short))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(short));
            if (value == null)
                return null;
        }

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
        return FromBytesToUInt16(value, false);
    }

    /// <summary>
    ///     Converts bytes to a 16-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 16-bit unsigned integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? FromBytesToUInt16(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(ushort))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(ushort));
            if (value == null)
                return null;
        }

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
        return FromBytesToInt32(value, false);
    }

    /// <summary>
    ///     Converts bytes to a 32-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 32-bit signed integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? FromBytesToInt32(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(int))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(int));
            if (value == null)
                return null;
        }

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
        return FromBytesToUInt32(value, false);
    }

    /// <summary>
    ///     Converts bytes to a 32-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 32-bit unsigned integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? FromBytesToUInt32(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(uint))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(uint));
            if (value == null)
                return null;
        }

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
        return FromBytesToInt64(value, false);
    }

    /// <summary>
    ///     Converts bytes to a 64-bit signed integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 64-bit signed integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromBytesToInt64(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(long))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(long));
            if (value == null)
                return null;
        }

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
        return FromBytesToUInt64(value, false);
    }

    /// <summary>
    ///     Converts bytes to a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted 64-bit unsigned integer, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? FromBytesToUInt64(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(ulong))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(ulong));
            if (value == null)
                return null;
        }

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
        return FromBytesToHalf(value, false);
    }

    /// <summary>
    ///     Converts bytes to a half-precision floating point value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>Converted half-precision floating point value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public Half? FromBytesToHalf(byte[] value, bool padIfNeeded)
    {
        const int halfSize = 2; 
        if (value == null)
            return null;

        if (value.Length < halfSize)
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, halfSize);
            if (value == null)
                return null;
        }

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
        return FromBytesToFloat(value, false);
    }

    /// <summary>
    ///     Converts bytes to a float value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>The float value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? FromBytesToFloat(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(float))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(float));
            if (value == null)
                return null;
        }

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
        return FromBytesToDouble(value, false);
    }

    /// <summary>
    ///     Converts bytes to a double value.
    /// </summary>
    /// <param name="value">Byte array containing the value to convert.</param>
    /// <param name="padIfNeeded">If true, pads the array with zeros if insufficient bytes; if false, returns null.</param>
    /// <returns>The double value, or null if insufficient bytes and padIfNeeded is false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? FromBytesToDouble(byte[] value, bool padIfNeeded)
    {
        if (value == null)
            return null;

        if (value.Length < sizeof(double))
        {
            if (!padIfNeeded)
                return null;

            value = PadBytes(value, sizeof(double));
            if (value == null)
                return null;
        }

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
