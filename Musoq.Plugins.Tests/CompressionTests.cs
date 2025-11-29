using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class CompressionTests : LibraryBaseBaseTests
{
    private const string TestXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><root><item>Hello World</item></root>";
    private const string TestText = "Hello, World! This is a test string for compression.";

    #region ZLib Tests

    [TestMethod]
    public void CompressZLib_WhenStringProvided_ShouldReturnCompressedBytes()
    {
        var compressed = Library.CompressZLib(TestText);

        Assert.IsNotNull(compressed);
        Assert.IsNotEmpty(compressed);
    }

    [TestMethod]
    public void CompressZLib_WhenNullString_ShouldReturnNull()
    {
        var compressed = Library.CompressZLib((string?)null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    public void CompressZLib_WhenBytesProvided_ShouldReturnCompressedBytes()
    {
        var data = Encoding.UTF8.GetBytes(TestText);
        var compressed = Library.CompressZLib(data);

        Assert.IsNotNull(compressed);
        Assert.IsNotEmpty(compressed);
    }

    [TestMethod]
    public void CompressZLib_WhenNullBytes_ShouldReturnNull()
    {
        var compressed = Library.CompressZLib((byte[]?)null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    public void DecompressZLib_WhenCompressedBytesProvided_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressZLib(TestText);
        var decompressed = Library.DecompressZLib(compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressZLib_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressZLib(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressZLib_WhenEmptyArrayProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressZLib([]);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressZLib_WithEncoding_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressZLib(TestText);
        var decompressed = Library.DecompressZLib(compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressZLibToBytes_WhenCompressedBytesProvided_ShouldReturnOriginalBytes()
    {
        var originalBytes = Encoding.UTF8.GetBytes(TestText);
        var compressed = Library.CompressZLib(originalBytes);
        var decompressed = Library.DecompressZLibToBytes(compressed);

        CollectionAssert.AreEqual(originalBytes, decompressed);
    }

    [TestMethod]
    public void CompressZLibToBase64_WhenStringProvided_ShouldReturnBase64String()
    {
        var base64Compressed = Library.CompressZLibToBase64(TestText);

        Assert.IsNotNull(base64Compressed);
        Assert.IsFalse(string.IsNullOrEmpty(base64Compressed));
        
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(base64Compressed);
        Assert.IsNotEmpty(bytes);
    }

    [TestMethod]
    public void CompressZLibToBase64_WhenNull_ShouldReturnNull()
    {
        var base64Compressed = Library.CompressZLibToBase64(null);

        Assert.IsNull(base64Compressed);
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WhenBase64Provided_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressZLibToBase64(TestText);
        var decompressed = Library.DecompressZLibFromBase64(base64Compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressZLibFromBase64(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WhenEmptyProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressZLibFromBase64(string.Empty);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WithEncoding_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressZLibToBase64(TestText);
        var decompressed = Library.DecompressZLibFromBase64(base64Compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressZLibFromBase64_WithXml_ShouldReturnOriginalXml()
    {
        var base64Compressed = Library.CompressZLibToBase64(TestXml);
        var decompressed = Library.DecompressZLibFromBase64(base64Compressed);

        Assert.AreEqual(TestXml, decompressed);
    }

    #endregion

    #region GZip Tests

    [TestMethod]
    public void CompressGZip_WhenStringProvided_ShouldReturnCompressedBytes()
    {
        var compressed = Library.CompressGZip(TestText);

        Assert.IsNotNull(compressed);
        Assert.IsNotEmpty(compressed);
    }

    [TestMethod]
    public void CompressGZip_WhenNullString_ShouldReturnNull()
    {
        var compressed = Library.CompressGZip((string?)null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    public void DecompressGZip_WhenCompressedBytesProvided_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressGZip(TestText);
        var decompressed = Library.DecompressGZip(compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressGZip_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressGZip(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressGZip_WithEncoding_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressGZip(TestText);
        var decompressed = Library.DecompressGZip(compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressGZipToBytes_WhenCompressedBytesProvided_ShouldReturnOriginalBytes()
    {
        var originalBytes = Encoding.UTF8.GetBytes(TestText);
        var compressed = Library.CompressGZip(originalBytes);
        var decompressed = Library.DecompressGZipToBytes(compressed);

        CollectionAssert.AreEqual(originalBytes, decompressed);
    }

    [TestMethod]
    public void CompressGZipToBase64_WhenStringProvided_ShouldReturnBase64String()
    {
        var base64Compressed = Library.CompressGZipToBase64(TestText);

        Assert.IsNotNull(base64Compressed);
        Assert.IsFalse(string.IsNullOrEmpty(base64Compressed));
    }

    [TestMethod]
    public void DecompressGZipFromBase64_WhenBase64Provided_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressGZipToBase64(TestText);
        var decompressed = Library.DecompressGZipFromBase64(base64Compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressGZipFromBase64_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressGZipFromBase64(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressGZipFromBase64_WithEncoding_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressGZipToBase64(TestText);
        var decompressed = Library.DecompressGZipFromBase64(base64Compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    #endregion

    #region Deflate Tests

    [TestMethod]
    public void CompressDeflate_WhenStringProvided_ShouldReturnCompressedBytes()
    {
        var compressed = Library.CompressDeflate(TestText);

        Assert.IsNotNull(compressed);
        Assert.IsNotEmpty(compressed);
    }

    [TestMethod]
    public void CompressDeflate_WhenNullString_ShouldReturnNull()
    {
        var compressed = Library.CompressDeflate((string?)null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    public void DecompressDeflate_WhenCompressedBytesProvided_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressDeflate(TestText);
        var decompressed = Library.DecompressDeflate(compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressDeflate_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressDeflate(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressDeflate_WithEncoding_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressDeflate(TestText);
        var decompressed = Library.DecompressDeflate(compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressDeflateToBytes_WhenCompressedBytesProvided_ShouldReturnOriginalBytes()
    {
        var originalBytes = Encoding.UTF8.GetBytes(TestText);
        var compressed = Library.CompressDeflate(originalBytes);
        var decompressed = Library.DecompressDeflateToBytes(compressed);

        CollectionAssert.AreEqual(originalBytes, decompressed);
    }

    [TestMethod]
    public void CompressDeflateToBase64_WhenStringProvided_ShouldReturnBase64String()
    {
        var base64Compressed = Library.CompressDeflateToBase64(TestText);

        Assert.IsNotNull(base64Compressed);
        Assert.IsFalse(string.IsNullOrEmpty(base64Compressed));
    }

    [TestMethod]
    public void DecompressDeflateFromBase64_WhenBase64Provided_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressDeflateToBase64(TestText);
        var decompressed = Library.DecompressDeflateFromBase64(base64Compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressDeflateFromBase64_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressDeflateFromBase64(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressDeflateFromBase64_WithEncoding_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressDeflateToBase64(TestText);
        var decompressed = Library.DecompressDeflateFromBase64(base64Compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    #endregion

    #region Round-trip Tests with Real XML

    [TestMethod]
    public void ZLibRoundTrip_WithXml_ShouldPreserveContent()
    {
        var xml = "<?xml version=\"1.0\"?><data><record id=\"1\"><name>Test</name><value>123</value></record></data>";
        
        var compressed = Library.CompressZLibToBase64(xml);
        var decompressed = Library.DecompressZLibFromBase64(compressed);

        Assert.AreEqual(xml, decompressed);
    }

    [TestMethod]
    public void GZipRoundTrip_WithXml_ShouldPreserveContent()
    {
        var xml = "<?xml version=\"1.0\"?><data><record id=\"1\"><name>Test</name><value>123</value></record></data>";
        
        var compressed = Library.CompressGZipToBase64(xml);
        var decompressed = Library.DecompressGZipFromBase64(compressed);

        Assert.AreEqual(xml, decompressed);
    }

    [TestMethod]
    public void DeflateRoundTrip_WithXml_ShouldPreserveContent()
    {
        var xml = "<?xml version=\"1.0\"?><data><record id=\"1\"><name>Test</name><value>123</value></record></data>";
        
        var compressed = Library.CompressDeflateToBase64(xml);
        var decompressed = Library.DecompressDeflateFromBase64(compressed);

        Assert.AreEqual(xml, decompressed);
    }

    [TestMethod]
    public void ZLibRoundTrip_WithUnicodeContent_ShouldPreserveContent()
    {
        var text = "Hello 世界! Привет мир! 🌍";
        
        var compressed = Library.CompressZLibToBase64(text);
        var decompressed = Library.DecompressZLibFromBase64(compressed);

        Assert.AreEqual(text, decompressed);
    }

    #endregion

    #region Brotli Tests

    [TestMethod]
    public void CompressBrotli_WhenStringProvided_ShouldReturnCompressedBytes()
    {
        var compressed = Library.CompressBrotli(TestText);

        Assert.IsNotNull(compressed);
        Assert.IsNotEmpty(compressed);
    }

    [TestMethod]
    public void CompressBrotli_WhenNullString_ShouldReturnNull()
    {
        var compressed = Library.CompressBrotli((string?)null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    public void DecompressBrotli_WhenCompressedBytesProvided_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressBrotli(TestText);
        var decompressed = Library.DecompressBrotli(compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressBrotli_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressBrotli(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressBrotli_WithEncoding_ShouldReturnOriginalString()
    {
        var compressed = Library.CompressBrotli(TestText);
        var decompressed = Library.DecompressBrotli(compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressBrotliToBytes_WhenCompressedBytesProvided_ShouldReturnOriginalBytes()
    {
        var originalBytes = Encoding.UTF8.GetBytes(TestText);
        var compressed = Library.CompressBrotli(originalBytes);
        var decompressed = Library.DecompressBrotliToBytes(compressed);

        CollectionAssert.AreEqual(originalBytes, decompressed);
    }

    [TestMethod]
    public void CompressBrotliToBase64_WhenStringProvided_ShouldReturnBase64String()
    {
        var base64Compressed = Library.CompressBrotliToBase64(TestText);

        Assert.IsNotNull(base64Compressed);
        Assert.IsFalse(string.IsNullOrEmpty(base64Compressed));
    }

    [TestMethod]
    public void DecompressBrotliFromBase64_WhenBase64Provided_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressBrotliToBase64(TestText);
        var decompressed = Library.DecompressBrotliFromBase64(base64Compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void DecompressBrotliFromBase64_WhenNullProvided_ShouldReturnNull()
    {
        var decompressed = Library.DecompressBrotliFromBase64(null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    public void DecompressBrotliFromBase64_WithEncoding_ShouldReturnOriginalString()
    {
        var base64Compressed = Library.CompressBrotliToBase64(TestText);
        var decompressed = Library.DecompressBrotliFromBase64(base64Compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    public void BrotliRoundTrip_WithXml_ShouldPreserveContent()
    {
        var xml = "<?xml version=\"1.0\"?><data><record id=\"1\"><name>Test</name><value>123</value></record></data>";
        
        var compressed = Library.CompressBrotliToBase64(xml);
        var decompressed = Library.DecompressBrotliFromBase64(compressed);

        Assert.AreEqual(xml, decompressed);
    }

    #endregion
}
