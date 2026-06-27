using System.IO.Compression;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{

    /// <summary>
    ///     Decompresses a Deflate-compressed byte array and returns it as a UTF-8 string.
    /// </summary>
    /// <param name="compressedData">The Deflate-compressed data</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressDeflate(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressToString(compressedData, Encoding.UTF8, s => new DeflateStream(s, CompressionMode.Decompress));
    }

    /// <summary>
    ///     Decompresses a Deflate-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    /// <param name="compressedData">The Deflate-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressDeflate(byte[]? compressedData, string encodingName)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressToString(compressedData, Encoding.GetEncoding(encodingName), s => new DeflateStream(s, CompressionMode.Decompress));
    }

    /// <summary>
    ///     Decompresses a Deflate-compressed byte array and returns the raw bytes.
    /// </summary>
    /// <param name="compressedData">The Deflate-compressed data</param>
    /// <returns>The decompressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? DecompressDeflateToBytes(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressToBytesCore(compressedData, s => new DeflateStream(s, CompressionMode.Decompress));
    }

    /// <summary>
    ///     Decompresses a base64-encoded, Deflate-compressed string and returns the decompressed text.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, Deflate-compressed data</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressDeflateFromBase64(string? base64CompressedData)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        return DecompressDeflate(Convert.FromBase64String(base64CompressedData));
    }

    /// <summary>
    ///     Decompresses a base64-encoded, Deflate-compressed string and returns the decompressed text using specified
    ///     encoding.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, Deflate-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressDeflateFromBase64(string? base64CompressedData, string encodingName)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        return DecompressDeflate(Convert.FromBase64String(base64CompressedData), encodingName);
    }

    /// <summary>
    ///     Compresses a string using Deflate compression and returns the compressed bytes.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The Deflate-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressDeflate(string? data)
    {
        if (data == null)
            return null;

        return CompressDeflate(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    ///     Compresses a byte array using Deflate compression.
    /// </summary>
    /// <param name="data">The data to compress</param>
    /// <returns>The Deflate-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressDeflate(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return null;

        return CompressCore(data, s => new DeflateStream(s, CompressionLevel.Optimal));
    }

    /// <summary>
    ///     Compresses a string using Deflate compression and returns the result as a base64-encoded string.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The base64-encoded, Deflate-compressed data, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? CompressDeflateToBase64(string? data)
    {
        if (data == null)
            return null;

        var compressedBytes = CompressDeflate(data);
        return compressedBytes != null ? Convert.ToBase64String(compressedBytes) : null;
    }
}
