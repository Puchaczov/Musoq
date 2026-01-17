using System.Security.Cryptography;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Helpers;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the md5 hash of the given string.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha256 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Md5(string? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, MD5.Create);
    }

    /// <summary>
    ///     Gets the md5 hash of the given bytes array.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha256 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Md5(byte[]? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, MD5.Create);
    }

    /// <summary>
    ///     Gets the sha256 hash of the given string.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha1 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha1(string? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, SHA1.Create);
    }

    /// <summary>
    ///     Gets the sha256 hash of the given bytes array.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha1 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha1(byte[]? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, SHA1.Create);
    }

    /// <summary>
    ///     Gets the sha256 hash of the given string.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha256 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha256(string? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, SHA256.Create);
    }

    /// <summary>
    ///     Gets the sha256 hash of the given bytes array.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha256 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha256(byte[]? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, SHA256.Create);
    }

    /// <summary>
    ///     Gets the sha256 hash of the given string.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha256 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha512(string? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, SHA512.Create);
    }

    /// <summary>
    ///     Gets the sha256 hash of the bytes array.
    /// </summary>
    /// <param name="content">The content string</param>
    /// <returns>The sha256 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Cryptography)]
    public string? Sha512(byte[]? content)
    {
        return content == null ? null : HashHelper.ComputeHash(content, SHA512.Create);
    }
}