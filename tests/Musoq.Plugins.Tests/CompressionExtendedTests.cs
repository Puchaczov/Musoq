using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for compression methods to improve branch coverage.
///     Tests ZLib, GZip, Deflate, Brotli compression and decompression.
/// </summary>
[TestClass]
public class CompressionExtendedTests : LibraryBaseBaseTests
{
    #region ZLib Tests

    [TestMethod]
    public void CompressZLib_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressZLib((string?)null));
    }

    [TestMethod]
    public void CompressZLib_ValidString_ReturnsCompressedBytes()
    {
        var result = Library.CompressZLib("hello world");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void CompressZLib_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressZLib((byte[]?)null));
    }

    [TestMethod]
    public void CompressZLib_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressZLib(Array.Empty<byte>()));
    }

    [TestMethod]
    public void CompressZLib_ValidBytes_ReturnsCompressedBytes()
    {
        var bytes = Encoding.UTF8.GetBytes("hello world");
        var result = Library.CompressZLib(bytes);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void DecompressZLib_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLib(null));
    }

    [TestMethod]
    public void DecompressZLib_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLib(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressZLib_ValidCompressedData_ReturnsDecompressed()
    {
        var original = "hello world test compression";
        var compressed = Library.CompressZLib(original);
        var decompressed = Library.DecompressZLib(compressed);
        Assert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void DecompressZLib_WithEncoding_ReturnsDecompressed()
    {
        var original = "hello world";
        var compressed = Library.CompressZLib(original);
        var decompressed = Library.DecompressZLib(compressed, "UTF-8");
        Assert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void DecompressZLib_WithEncoding_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLib(null, "UTF-8"));
    }

    [TestMethod]
    public void DecompressZLibToBytes_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLibToBytes(null));
    }

    [TestMethod]
    public void DecompressZLibToBytes_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLibToBytes(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressZLibToBytes_ValidData_ReturnsBytes()
    {
        var original = Encoding.UTF8.GetBytes("hello world");
        var compressed = Library.CompressZLib(original);
        var decompressed = Library.DecompressZLibToBytes(compressed);
        CollectionAssert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void CompressZLibToBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressZLibToBase64(null));
    }

    [TestMethod]
    public void CompressZLibToBase64_ValidString_ReturnsBase64()
    {
        var result = Library.CompressZLibToBase64("hello world");
        Assert.IsNotNull(result);

        var decoded = Convert.FromBase64String(result);
        Assert.IsTrue(decoded.Length > 0);
    }

    [TestMethod]
    public void DecompressZLibFromBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLibFromBase64(null));
    }

    [TestMethod]
    public void DecompressZLibFromBase64_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLibFromBase64(string.Empty));
    }

    [TestMethod]
    public void DecompressZLibFromBase64_ValidData_ReturnsDecompressed()
    {
        var original = "hello world";
        var compressed = Library.CompressZLibToBase64(original);
        var decompressed = Library.DecompressZLibFromBase64(compressed);
        Assert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WithEncoding_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLibFromBase64(null, "UTF-8"));
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WithEncoding_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressZLibFromBase64(string.Empty, "UTF-8"));
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WithEncoding_ValidData_ReturnsDecompressed()
    {
        var original = "hello world";
        var compressed = Library.CompressZLibToBase64(original);
        var decompressed = Library.DecompressZLibFromBase64(compressed, "UTF-8");
        Assert.AreEqual(original, decompressed);
    }

    #endregion

    #region GZip Tests

    [TestMethod]
    public void CompressGZip_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressGZip((string?)null));
    }

    [TestMethod]
    public void CompressGZip_ValidString_ReturnsCompressedBytes()
    {
        var result = Library.CompressGZip("hello world");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void CompressGZip_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressGZip((byte[]?)null));
    }

    [TestMethod]
    public void CompressGZip_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressGZip(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressGZip_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressGZip(null));
    }

    [TestMethod]
    public void DecompressGZip_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressGZip(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressGZip_ValidData_ReturnsDecompressed()
    {
        var original = "hello world test compression";
        var compressed = Library.CompressGZip(original);
        var decompressed = Library.DecompressGZip(compressed);
        Assert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void DecompressGZip_WithEncoding_ReturnsDecompressed()
    {
        var original = "hello world";
        var compressed = Library.CompressGZip(original);
        var decompressed = Library.DecompressGZip(compressed, "UTF-8");
        Assert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void DecompressGZipToBytes_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressGZipToBytes(null));
    }

    [TestMethod]
    public void DecompressGZipToBytes_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressGZipToBytes(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressGZipToBytes_ValidData_ReturnsBytes()
    {
        var original = Encoding.UTF8.GetBytes("hello world");
        var compressed = Library.CompressGZip(original);
        var decompressed = Library.DecompressGZipToBytes(compressed);
        CollectionAssert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void CompressGZipToBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressGZipToBase64(null));
    }

    [TestMethod]
    public void CompressGZipToBase64_ValidString_ReturnsBase64()
    {
        var result = Library.CompressGZipToBase64("hello world");
        Assert.IsNotNull(result);
        var decoded = Convert.FromBase64String(result);
        Assert.IsTrue(decoded.Length > 0);
    }

    [TestMethod]
    public void DecompressGZipFromBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressGZipFromBase64(null));
    }

    [TestMethod]
    public void DecompressGZipFromBase64_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressGZipFromBase64(string.Empty));
    }

    [TestMethod]
    public void DecompressGZipFromBase64_ValidData_ReturnsDecompressed()
    {
        var original = "hello world";
        var compressed = Library.CompressGZipToBase64(original);
        var decompressed = Library.DecompressGZipFromBase64(compressed);
        Assert.AreEqual(original, decompressed);
    }

    #endregion

    #region Deflate Tests

    [TestMethod]
    public void CompressDeflate_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressDeflate((string?)null));
    }

    [TestMethod]
    public void CompressDeflate_ValidString_ReturnsCompressedBytes()
    {
        var result = Library.CompressDeflate("hello world");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void CompressDeflate_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressDeflate((byte[]?)null));
    }

    [TestMethod]
    public void CompressDeflate_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressDeflate(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressDeflate_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressDeflate(null));
    }

    [TestMethod]
    public void DecompressDeflate_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressDeflate(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressDeflate_ValidData_ReturnsDecompressed()
    {
        var original = "hello world test compression";
        var compressed = Library.CompressDeflate(original);
        var decompressed = Library.DecompressDeflate(compressed);
        Assert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void DecompressDeflateToBytes_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressDeflateToBytes(null));
    }

    [TestMethod]
    public void DecompressDeflateToBytes_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressDeflateToBytes(Array.Empty<byte>()));
    }

    [TestMethod]
    public void CompressDeflateToBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressDeflateToBase64(null));
    }

    [TestMethod]
    public void CompressDeflateToBase64_ValidString_ReturnsBase64()
    {
        var result = Library.CompressDeflateToBase64("hello world");
        Assert.IsNotNull(result);
        var decoded = Convert.FromBase64String(result);
        Assert.IsTrue(decoded.Length > 0);
    }

    [TestMethod]
    public void DecompressDeflateFromBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressDeflateFromBase64(null));
    }

    [TestMethod]
    public void DecompressDeflateFromBase64_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressDeflateFromBase64(string.Empty));
    }

    #endregion

    #region Brotli Tests

    [TestMethod]
    public void CompressBrotli_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressBrotli((string?)null));
    }

    [TestMethod]
    public void CompressBrotli_ValidString_ReturnsCompressedBytes()
    {
        var result = Library.CompressBrotli("hello world");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void CompressBrotli_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressBrotli((byte[]?)null));
    }

    [TestMethod]
    public void CompressBrotli_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.CompressBrotli(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressBrotli_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressBrotli(null));
    }

    [TestMethod]
    public void DecompressBrotli_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressBrotli(Array.Empty<byte>()));
    }

    [TestMethod]
    public void DecompressBrotli_ValidData_ReturnsDecompressed()
    {
        var original = "hello world test compression";
        var compressed = Library.CompressBrotli(original);
        var decompressed = Library.DecompressBrotli(compressed);
        Assert.AreEqual(original, decompressed);
    }

    [TestMethod]
    public void DecompressBrotliToBytes_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressBrotliToBytes(null));
    }

    [TestMethod]
    public void DecompressBrotliToBytes_EmptyBytes_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressBrotliToBytes(Array.Empty<byte>()));
    }

    [TestMethod]
    public void CompressBrotliToBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.CompressBrotliToBase64(null));
    }

    [TestMethod]
    public void CompressBrotliToBase64_ValidString_ReturnsBase64()
    {
        var result = Library.CompressBrotliToBase64("hello world");
        Assert.IsNotNull(result);
        var decoded = Convert.FromBase64String(result);
        Assert.IsTrue(decoded.Length > 0);
    }

    [TestMethod]
    public void DecompressBrotliFromBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressBrotliFromBase64(null));
    }

    [TestMethod]
    public void DecompressBrotliFromBase64_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.DecompressBrotliFromBase64(string.Empty));
    }

    #endregion
}
