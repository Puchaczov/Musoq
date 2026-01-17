using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Plugins.Helpers;

/// <summary>
///     This class is used to calculate hashes of a string.
/// </summary>
public static class HashHelper
{
    /// <summary>
    ///     Computes the hash of a string using the THashProvider algorithm.
    /// </summary>
    /// <param name="content">The content</param>
    /// <param name="create">The provider</param>
    /// <typeparam name="THashProvider">The provider</typeparam>
    /// <returns>Hash of a value</returns>
    public static string ComputeHash<THashProvider>(string content, Func<THashProvider> create)
        where THashProvider : HashAlgorithm
    {
        return ComputeHash(Encoding.UTF8.GetBytes(content), create);
    }

    /// <summary>
    ///     Computes the hash of a string using the THashProvider algorithm.
    /// </summary>
    /// <param name="content">The content</param>
    /// <param name="create">The provider</param>
    /// <typeparam name="THashProvider">The provider</typeparam>
    /// <returns>Hash of a value</returns>
    public static string ComputeHash<THashProvider>(byte[] content, Func<THashProvider> create)
        where THashProvider : HashAlgorithm
    {
        using var stream = new MemoryStream(content);
        return ComputeHash(stream, create);
    }

    /// <summary>
    ///     Computes the hash of a string using the THashProvider algorithm.
    /// </summary>
    /// <param name="stream">The content</param>
    /// <param name="create">The provider</param>
    /// <typeparam name="THashProvider">The provider</typeparam>
    /// <returns>Hash of a value</returns>
    public static string ComputeHash<THashProvider>(Stream stream, Func<THashProvider> create)
        where THashProvider : HashAlgorithm
    {
        using var hashProvider = create();
        var hash = hashProvider.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}