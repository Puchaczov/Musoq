using Musoq.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Gets the bytes from the given string. 
    /// </summary>
    /// <param name="content">The string</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(string? content)
    {
        if (content == null)
            return null;

        return Encoding.UTF8.GetBytes(content);
    }

    /// <summary>
    /// Gets the bytes from the given string within given offset and length. 
    /// </summary>
    /// <param name="content">The string</param>
    /// <param name="length">The length of substring</param>
    /// <param name="offset">The offset of substring</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(string? content, int length, int offset)
    {
        if (content == null)
            return null;

        return Encoding.UTF8.GetBytes(content.Substring(offset, length));
    }

    /// <summary>
    /// Gets the bytes from the given character. 
    /// </summary>
    /// <param name="character">The character to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(char? character)
    {
        if (character == null)
            return null;

        return BitConverter.GetBytes(character.Value);
    }

    /// <summary>
    /// Gets the bytes from the given boolean. 
    /// </summary>
    /// <param name="bit">The boolean to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(bool? bit)
    {
        if (bit == null)
            return null;

        return BitConverter.GetBytes(bit.Value);
    }

    /// <summary>
    /// Gets the bytes from the given long. 
    /// </summary>
    /// <param name="value">The long to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(long? value)
    {
        if (value == null)
            return null;

        return BitConverter.GetBytes(value.Value);
    }

    /// <summary>
    /// Gets the bytes from the given int. 
    /// </summary>
    /// <param name="value">The int to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(int? value)
    {
        if (value == null)
            return null;

        return BitConverter.GetBytes(value.Value);
    }

    /// <summary>
    /// Gets the bytes from the given short. 
    /// </summary>
    /// <param name="value">The short to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(short? value)
    {
        if (value == null)
            return null;

        return BitConverter.GetBytes(value.Value);
    }

    /// <summary>
    /// Gets the bytes from the given ulong. 
    /// </summary>
    /// <param name="value">The ulong to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(ulong? value)
    {
        if (value == null)
            return null;

        return BitConverter.GetBytes(value.Value);
    }

    /// <summary>
    /// Gets the bytes from the given ushort. 
    /// </summary>
    /// <param name="value">The ushort to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(ushort? value)
    {
        if (value == null)
            return null;

        return BitConverter.GetBytes(value.Value);
    }

    /// <summary>
    /// Gets the bytes from the given uint. 
    /// </summary>
    /// <param name="value">The uint to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(uint? value)
    {
        if (value == null)
            return null;

        return BitConverter.GetBytes(value.Value);
    }

    /// <summary>
    /// Gets the bytes from the given decimal. 
    /// </summary>
    /// <param name="value">The decimal to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(decimal? value)
    {
        if (value == null)
            return null;

        var bytes = new List<byte>();

        foreach (var integerValue in decimal.GetBits(value.Value))
            bytes.AddRange(BitConverter.GetBytes(integerValue));

        return bytes.ToArray();
    }

    /// <summary>
    /// Gets the bytes from the given double. 
    /// </summary>
    /// <param name="value">The double to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(double? value)
    {
        if (value == null)
            return null;

        return GetBytes(BitConverter.DoubleToInt64Bits(value.Value));
    }

    /// <summary>
    /// Gets the bytes from the given float. 
    /// </summary>
    /// <param name="value">The float to convert to bytes</param>
    /// <returns>Bytes of a given content</returns>
    [BindableMethod]
    public byte[]? GetBytes(float? value)
    {
        if (value == null)
            return null;

        return BitConverter.GetBytes(value.Value);
    }
}