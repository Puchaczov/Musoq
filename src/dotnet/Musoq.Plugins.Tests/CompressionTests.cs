using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class CompressionTests : PluginsTestBase
{
    private const string ZLib = "ZLib";
    private const string GZip = "GZip";
    private const string Deflate = "Deflate";
    private const string Brotli = "Brotli";
    private const string TestXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><root><item>Hello World</item></root>";
    private const string TestText = "Hello, World! This is a test string for compression.";
    private const string UnicodeText = "Hello \u4e16\u754c! \u041f\u0440\u0438\u0432\u0435\u0442 \u043c\u0438\u0440! \U0001f30d";

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void CompressString_WhenValueProvided_ReturnsCompressedBytes(string codec)
    {
        var compressed = CompressString(codec, TestText);

        AssertCompressedBytes(compressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void CompressString_WhenNullProvided_ReturnsNull(string codec)
    {
        var compressed = CompressString(codec, null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void CompressBytes_WhenValueProvided_ReturnsCompressedBytes(string codec)
    {
        var data = Encoding.UTF8.GetBytes(TestText);
        var compressed = CompressBytes(codec, data);

        AssertCompressedBytes(compressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void CompressBytes_WhenNullProvided_ReturnsNull(string codec)
    {
        var compressed = CompressBytes(codec, null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void CompressBytes_WhenEmptyProvided_ReturnsNull(string codec)
    {
        var compressed = CompressBytes(codec, []);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void Decompress_WhenNullProvided_ReturnsNull(string codec)
    {
        var decompressed = Decompress(codec, null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void Decompress_WhenEmptyProvided_ReturnsNull(string codec)
    {
        var decompressed = Decompress(codec, []);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressWithEncoding_WhenNullProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressWithEncoding(codec, null, "UTF-8");

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressWithEncoding_WhenEmptyProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressWithEncoding(codec, [], "UTF-8");

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressToBytes_WhenNullProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressToBytes(codec, null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressToBytes_WhenEmptyProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressToBytes(codec, []);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void RoundTrip_WhenStringCompressed_ReturnsOriginalString(string codec)
    {
        var compressed = CompressString(codec, TestText);
        var decompressed = Decompress(codec, compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void RoundTrip_WhenEncodingProvided_ReturnsOriginalString(string codec)
    {
        var compressed = CompressString(codec, TestText);
        var decompressed = DecompressWithEncoding(codec, compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void RoundTrip_WhenBytesCompressed_ReturnsOriginalBytes(string codec)
    {
        var originalBytes = Encoding.UTF8.GetBytes(TestText);
        var compressed = CompressBytes(codec, originalBytes);
        var decompressed = DecompressToBytes(codec, compressed);

        CollectionAssert.AreEqual(originalBytes, decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void CompressToBase64_WhenStringProvided_ReturnsBase64(string codec)
    {
        var compressed = CompressToBase64(codec, TestText);

        AssertBase64Payload(compressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void CompressToBase64_WhenNullProvided_ReturnsNull(string codec)
    {
        var compressed = CompressToBase64(codec, null);

        Assert.IsNull(compressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressFromBase64_WhenNullProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressFromBase64(codec, null);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressFromBase64_WhenEmptyProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressFromBase64(codec, string.Empty);

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressFromBase64WithEncoding_WhenNullProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressFromBase64WithEncoding(codec, null, "UTF-8");

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void DecompressFromBase64WithEncoding_WhenEmptyProvided_ReturnsNull(string codec)
    {
        var decompressed = DecompressFromBase64WithEncoding(codec, string.Empty, "UTF-8");

        Assert.IsNull(decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void Base64RoundTrip_WhenStringCompressed_ReturnsOriginalString(string codec)
    {
        var compressed = CompressToBase64(codec, TestText);
        var decompressed = DecompressFromBase64(codec, compressed);

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void Base64RoundTrip_WhenEncodingProvided_ReturnsOriginalString(string codec)
    {
        var compressed = CompressToBase64(codec, TestText);
        var decompressed = DecompressFromBase64WithEncoding(codec, compressed, "UTF-8");

        Assert.AreEqual(TestText, decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void Base64RoundTrip_WhenXmlCompressed_ReturnsOriginalXml(string codec)
    {
        var compressed = CompressToBase64(codec, TestXml);
        var decompressed = DecompressFromBase64(codec, compressed);

        Assert.AreEqual(TestXml, decompressed);
    }

    [TestMethod]
    [DynamicData(nameof(Codecs))]
    public void Base64RoundTrip_WhenUnicodeCompressed_ReturnsOriginalText(string codec)
    {
        var compressed = CompressToBase64(codec, UnicodeText);
        var decompressed = DecompressFromBase64(codec, compressed);

        Assert.AreEqual(UnicodeText, decompressed);
    }

    public static IEnumerable<object[]> Codecs()
    {
        yield return [ZLib];
        yield return [GZip];
        yield return [Deflate];
        yield return [Brotli];
    }

    private byte[]? CompressString(string codec, string? data)
    {
        return codec switch
        {
            ZLib => Library.CompressZLib(data),
            GZip => Library.CompressGZip(data),
            Deflate => Library.CompressDeflate(data),
            Brotli => Library.CompressBrotli(data),
            _ => throw UnknownCodec(codec)
        };
    }

    private byte[]? CompressBytes(string codec, byte[]? data)
    {
        return codec switch
        {
            ZLib => Library.CompressZLib(data),
            GZip => Library.CompressGZip(data),
            Deflate => Library.CompressDeflate(data),
            Brotli => Library.CompressBrotli(data),
            _ => throw UnknownCodec(codec)
        };
    }

    private string? Decompress(string codec, byte[]? data)
    {
        return codec switch
        {
            ZLib => Library.DecompressZLib(data),
            GZip => Library.DecompressGZip(data),
            Deflate => Library.DecompressDeflate(data),
            Brotli => Library.DecompressBrotli(data),
            _ => throw UnknownCodec(codec)
        };
    }

    private string? DecompressWithEncoding(string codec, byte[]? data, string encoding)
    {
        return codec switch
        {
            ZLib => Library.DecompressZLib(data, encoding),
            GZip => Library.DecompressGZip(data, encoding),
            Deflate => Library.DecompressDeflate(data, encoding),
            Brotli => Library.DecompressBrotli(data, encoding),
            _ => throw UnknownCodec(codec)
        };
    }

    private byte[]? DecompressToBytes(string codec, byte[]? data)
    {
        return codec switch
        {
            ZLib => Library.DecompressZLibToBytes(data),
            GZip => Library.DecompressGZipToBytes(data),
            Deflate => Library.DecompressDeflateToBytes(data),
            Brotli => Library.DecompressBrotliToBytes(data),
            _ => throw UnknownCodec(codec)
        };
    }

    private string? CompressToBase64(string codec, string? data)
    {
        return codec switch
        {
            ZLib => Library.CompressZLibToBase64(data),
            GZip => Library.CompressGZipToBase64(data),
            Deflate => Library.CompressDeflateToBase64(data),
            Brotli => Library.CompressBrotliToBase64(data),
            _ => throw UnknownCodec(codec)
        };
    }

    private string? DecompressFromBase64(string codec, string? data)
    {
        return codec switch
        {
            ZLib => Library.DecompressZLibFromBase64(data),
            GZip => Library.DecompressGZipFromBase64(data),
            Deflate => Library.DecompressDeflateFromBase64(data),
            Brotli => Library.DecompressBrotliFromBase64(data),
            _ => throw UnknownCodec(codec)
        };
    }

    private string? DecompressFromBase64WithEncoding(string codec, string? data, string encoding)
    {
        return codec switch
        {
            ZLib => Library.DecompressZLibFromBase64(data, encoding),
            GZip => Library.DecompressGZipFromBase64(data, encoding),
            Deflate => Library.DecompressDeflateFromBase64(data, encoding),
            Brotli => Library.DecompressBrotliFromBase64(data, encoding),
            _ => throw UnknownCodec(codec)
        };
    }

    private static void AssertCompressedBytes(byte[]? compressed)
    {
        Assert.IsNotNull(compressed);
        Assert.IsNotEmpty(compressed);
    }

    private static void AssertBase64Payload(string? compressed)
    {
        Assert.IsNotNull(compressed);
        var bytes = Convert.FromBase64String(compressed);
        Assert.IsNotEmpty(bytes);
    }

    private static ArgumentOutOfRangeException UnknownCodec(string codec)
    {
        return new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown compression codec.");
    }
}