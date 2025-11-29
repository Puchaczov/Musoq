using System;
using System.Security.Cryptography;
using System.Text;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Helpers;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Computes the SHA-384 hash of a string.
    /// </summary>
    /// <param name="value">The string to hash</param>
    /// <returns>The SHA-384 hash as a hex string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha384(string? value)
    {
        return value == null ? null : HashHelper.ComputeHash(value, SHA384.Create);
    }

    /// <summary>
    /// Computes the SHA-384 hash of a byte array.
    /// </summary>
    /// <param name="value">The byte array to hash</param>
    /// <returns>The SHA-384 hash as a hex string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha384(byte[]? value)
    {
        return value == null ? null : HashHelper.ComputeHash(value, SHA384.Create);
    }

    /// <summary>
    /// Computes the CRC32 checksum of a string.
    /// </summary>
    /// <param name="value">The string to compute checksum for</param>
    /// <returns>The CRC32 checksum as a hex string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Crc32(string? value)
    {
        if (value == null)
            return null;

        var bytes = Encoding.UTF8.GetBytes(value);
        return ComputeCrc32(bytes);
    }

    /// <summary>
    /// Computes the CRC32 checksum of a byte array.
    /// </summary>
    /// <param name="value">The byte array to compute checksum for</param>
    /// <returns>The CRC32 checksum as a hex string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Crc32(byte[]? value)
    {
        if (value == null)
            return null;

        return ComputeCrc32(value);
    }

    private static string ComputeCrc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }
        return (~crc).ToString("x8");
    }

    /// <summary>
    /// Computes HMAC-SHA256 of a message using a key.
    /// </summary>
    /// <param name="message">The message to authenticate</param>
    /// <param name="key">The secret key</param>
    /// <returns>The HMAC-SHA256 as a hex string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? HmacSha256(string? message, string? key)
    {
        if (message == null || key == null)
            return null;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Computes HMAC-SHA512 of a message using a key.
    /// </summary>
    /// <param name="message">The message to authenticate</param>
    /// <param name="key">The secret key</param>
    /// <returns>The HMAC-SHA512 as a hex string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? HmacSha512(string? message, string? key)
    {
        if (message == null || key == null)
            return null;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
