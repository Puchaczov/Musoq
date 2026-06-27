using System.IO.Compression;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{

    /// <summary>
    ///     Decompresses a Brotli-compressed byte array and returns it as a UTF-8 string.
    ///     Brotli is commonly used in web APIs and HTTP responses.
    /// </summary>
    /// <param name="compressedData">The Brotli-compressed data</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressBrotli(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressToString(compressedData, Encoding.UTF8, s => new BrotliStream(s, CompressionMode.Decompress));
    }

    /// <summary>
    ///     Decompresses a Brotli-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    /// <param name="compressedData">The Brotli-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressBrotli(byte[]? compressedData, string encodingName)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressToString(compressedData, Encoding.GetEncoding(encodingName), s => new BrotliStream(s, CompressionMode.Decompress));
    }

    /// <summary>
    ///     Decompresses a Brotli-compressed byte array and returns the raw bytes.
    /// </summary>
    /// <param name="compressedData">The Brotli-compressed data</param>
    /// <returns>The decompressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? DecompressBrotliToBytes(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressToBytesCore(compressedData, s => new BrotliStream(s, CompressionMode.Decompress));
    }

    /// <summary>
    ///     Decompresses a base64-encoded, Brotli-compressed string and returns the decompressed text.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, Brotli-compressed data</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressBrotliFromBase64(string? base64CompressedData)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        return DecompressBrotli(Convert.FromBase64String(base64CompressedData));
    }

    /// <summary>
    ///     Decompresses a base64-encoded, Brotli-compressed string and returns the decompressed text using specified encoding.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, Brotli-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressBrotliFromBase64(string? base64CompressedData, string encodingName)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        return DecompressBrotli(Convert.FromBase64String(base64CompressedData), encodingName);
    }

    /// <summary>
    ///     Compresses a string using Brotli compression and returns the compressed bytes.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The Brotli-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressBrotli(string? data)
    {
        if (data == null)
            return null;

        return CompressBrotli(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    ///     Compresses a byte array using Brotli compression.
    /// </summary>
    /// <param name="data">The data to compress</param>
    /// <returns>The Brotli-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressBrotli(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return null;

        return CompressCore(data, s => new BrotliStream(s, CompressionLevel.Optimal));
    }

    /// <summary>
    ///     Compresses a string using Brotli compression and returns the result as a base64-encoded string.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The base64-encoded, Brotli-compressed data, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? CompressBrotliToBase64(string? data)
    {
        if (data == null)
            return null;

        var compressedBytes = CompressBrotli(data);
        return compressedBytes != null ? Convert.ToBase64String(compressedBytes) : null;
    }
}
