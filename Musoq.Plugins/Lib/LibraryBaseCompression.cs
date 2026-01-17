using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Decompresses a zlib-compressed byte array and returns it as a UTF-8 string.
    /// </summary>
    /// <param name="compressedData">The zlib-compressed data</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressZLib(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressZLib(compressedData, Encoding.UTF8);
    }

    /// <summary>
    ///     Decompresses a zlib-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    /// <param name="compressedData">The zlib-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressZLib(byte[]? compressedData, string encodingName)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        var encoding = Encoding.GetEncoding(encodingName);
        return DecompressZLib(compressedData, encoding);
    }

    /// <summary>
    ///     Decompresses a zlib-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    /// <param name="compressedData">The zlib-compressed data</param>
    /// <param name="encoding">The encoding to use</param>
    /// <returns>The decompressed string</returns>
    private static string DecompressZLib(byte[] compressedData, Encoding encoding)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        zlibStream.CopyTo(outputStream);
        return encoding.GetString(outputStream.ToArray());
    }

    /// <summary>
    ///     Decompresses a zlib-compressed byte array and returns the raw bytes.
    /// </summary>
    /// <param name="compressedData">The zlib-compressed data</param>
    /// <returns>The decompressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? DecompressZLibToBytes(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        using var inputStream = new MemoryStream(compressedData);
        using var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        zlibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Decompresses a base64-encoded, zlib-compressed string and returns the decompressed text.
    ///     This is a convenience method for handling data that is base64-encoded zlib content.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, zlib-compressed data</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressZLibFromBase64(string? base64CompressedData)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressZLib(compressedBytes);
    }

    /// <summary>
    ///     Decompresses a base64-encoded, zlib-compressed string and returns the decompressed text using specified encoding.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, zlib-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressZLibFromBase64(string? base64CompressedData, string encodingName)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressZLib(compressedBytes, encodingName);
    }

    /// <summary>
    ///     Compresses a string using zlib compression and returns the compressed bytes.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The zlib-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressZLib(string? data)
    {
        if (data == null)
            return null;

        return CompressZLib(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    ///     Compresses a byte array using zlib compression.
    /// </summary>
    /// <param name="data">The data to compress</param>
    /// <returns>The zlib-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressZLib(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return null;

        using var outputStream = new MemoryStream();
        using (var zlibStream = new ZLibStream(outputStream, CompressionLevel.Optimal))
        {
            zlibStream.Write(data, 0, data.Length);
        }

        return outputStream.ToArray();
    }

    /// <summary>
    ///     Compresses a string using zlib compression and returns the result as a base64-encoded string.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The base64-encoded, zlib-compressed data, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? CompressZLibToBase64(string? data)
    {
        if (data == null)
            return null;

        var compressedBytes = CompressZLib(data);
        return compressedBytes != null ? Convert.ToBase64String(compressedBytes) : null;
    }

    /// <summary>
    ///     Decompresses a GZip-compressed byte array and returns it as a UTF-8 string.
    /// </summary>
    /// <param name="compressedData">The GZip-compressed data</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressGZip(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        return DecompressGZip(compressedData, Encoding.UTF8);
    }

    /// <summary>
    ///     Decompresses a GZip-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    /// <param name="compressedData">The GZip-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressGZip(byte[]? compressedData, string encodingName)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        var encoding = Encoding.GetEncoding(encodingName);
        return DecompressGZip(compressedData, encoding);
    }

    /// <summary>
    ///     Decompresses a GZip-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    private static string DecompressGZip(byte[] compressedData, Encoding encoding)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        gzipStream.CopyTo(outputStream);
        return encoding.GetString(outputStream.ToArray());
    }

    /// <summary>
    ///     Decompresses a GZip-compressed byte array and returns the raw bytes.
    /// </summary>
    /// <param name="compressedData">The GZip-compressed data</param>
    /// <returns>The decompressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? DecompressGZipToBytes(byte[]? compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return null;

        using var inputStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Decompresses a base64-encoded, GZip-compressed string and returns the decompressed text.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, GZip-compressed data</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressGZipFromBase64(string? base64CompressedData)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressGZip(compressedBytes);
    }

    /// <summary>
    ///     Decompresses a base64-encoded, GZip-compressed string and returns the decompressed text using specified encoding.
    /// </summary>
    /// <param name="base64CompressedData">The base64-encoded, GZip-compressed data</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>The decompressed string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? DecompressGZipFromBase64(string? base64CompressedData, string encodingName)
    {
        if (string.IsNullOrEmpty(base64CompressedData))
            return null;

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressGZip(compressedBytes, encodingName);
    }

    /// <summary>
    ///     Compresses a string using GZip compression and returns the compressed bytes.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The GZip-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressGZip(string? data)
    {
        if (data == null)
            return null;

        return CompressGZip(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    ///     Compresses a byte array using GZip compression.
    /// </summary>
    /// <param name="data">The data to compress</param>
    /// <returns>The GZip-compressed bytes, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public byte[]? CompressGZip(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return null;

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        return outputStream.ToArray();
    }

    /// <summary>
    ///     Compresses a string using GZip compression and returns the result as a base64-encoded string.
    /// </summary>
    /// <param name="data">The string to compress</param>
    /// <returns>The base64-encoded, GZip-compressed data, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Compression)]
    public string? CompressGZipToBase64(string? data)
    {
        if (data == null)
            return null;

        var compressedBytes = CompressGZip(data);
        return compressedBytes != null ? Convert.ToBase64String(compressedBytes) : null;
    }

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

        return DecompressDeflate(compressedData, Encoding.UTF8);
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

        var encoding = Encoding.GetEncoding(encodingName);
        return DecompressDeflate(compressedData, encoding);
    }

    /// <summary>
    ///     Decompresses a Deflate-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    private static string DecompressDeflate(byte[] compressedData, Encoding encoding)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        deflateStream.CopyTo(outputStream);
        return encoding.GetString(outputStream.ToArray());
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

        using var inputStream = new MemoryStream(compressedData);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        deflateStream.CopyTo(outputStream);
        return outputStream.ToArray();
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

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressDeflate(compressedBytes);
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

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressDeflate(compressedBytes, encodingName);
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

        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        return outputStream.ToArray();
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

    #region Brotli Compression

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

        return DecompressBrotli(compressedData, Encoding.UTF8);
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

        var encoding = Encoding.GetEncoding(encodingName);
        return DecompressBrotli(compressedData, encoding);
    }

    /// <summary>
    ///     Decompresses a Brotli-compressed byte array and returns it as a string using the specified encoding.
    /// </summary>
    private static string DecompressBrotli(byte[] compressedData, Encoding encoding)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        brotliStream.CopyTo(outputStream);
        return encoding.GetString(outputStream.ToArray());
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

        using var inputStream = new MemoryStream(compressedData);
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        brotliStream.CopyTo(outputStream);
        return outputStream.ToArray();
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

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressBrotli(compressedBytes);
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

        var compressedBytes = Convert.FromBase64String(base64CompressedData);
        return DecompressBrotli(compressedBytes, encodingName);
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

        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, CompressionLevel.Optimal))
        {
            brotliStream.Write(data, 0, data.Length);
        }

        return outputStream.ToArray();
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

    #endregion
}