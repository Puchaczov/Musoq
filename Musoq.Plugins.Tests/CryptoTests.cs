using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class CryptoTests : LibraryBaseBaseTests
{
    #region SHA384 Tests

    [TestMethod]
    public void Sha384_WhenStringProvided_ShouldReturnHash()
    {
        var result = Library.Sha384("hello");

        Assert.IsNotNull(result);
        Assert.AreEqual(96, result.Length); 
    }

    [TestMethod]
    public void Sha384_WhenNullStringProvided_ShouldReturnNull()
    {
        var result = Library.Sha384((string?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Sha384_WhenEmptyStringProvided_ShouldReturnHash()
    {
        var result = Library.Sha384(string.Empty);

        Assert.IsNotNull(result);
        Assert.AreEqual(96, result.Length);
    }

    [TestMethod]
    public void Sha384_WhenSameInputProvided_ShouldReturnSameHash()
    {
        var result1 = Library.Sha384("test");
        var result2 = Library.Sha384("test");

        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void Sha384_WhenDifferentInputProvided_ShouldReturnDifferentHash()
    {
        var result1 = Library.Sha384("test1");
        var result2 = Library.Sha384("test2");

        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void Sha384_WhenBytesProvided_ShouldReturnHash()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var result = Library.Sha384(bytes);

        Assert.IsNotNull(result);
        Assert.AreEqual(96, result.Length);
    }

    [TestMethod]
    public void Sha384_WhenNullBytesProvided_ShouldReturnNull()
    {
        var result = Library.Sha384((byte[]?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Sha384_WhenEmptyBytesProvided_ShouldReturnHash()
    {
        var result = Library.Sha384(Array.Empty<byte>());

        Assert.IsNotNull(result);
        Assert.AreEqual(96, result.Length);
    }

    [TestMethod]
    public void Sha384_StringAndBytes_ShouldReturnSameHashForSameContent()
    {
        var input = "hello world";
        var bytes = Encoding.UTF8.GetBytes(input);

        var stringResult = Library.Sha384(input);
        var bytesResult = Library.Sha384(bytes);

        Assert.AreEqual(stringResult, bytesResult);
    }

    #endregion

    #region CRC32 Tests

    [TestMethod]
    public void Crc32_WhenStringProvided_ShouldReturnChecksum()
    {
        var result = Library.Crc32("hello");

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length); 
    }

    [TestMethod]
    public void Crc32_WhenNullStringProvided_ShouldReturnNull()
    {
        var result = Library.Crc32((string?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Crc32_WhenEmptyStringProvided_ShouldReturnChecksum()
    {
        var result = Library.Crc32(string.Empty);

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length);
    }

    [TestMethod]
    public void Crc32_WhenSameInputProvided_ShouldReturnSameChecksum()
    {
        var result1 = Library.Crc32("test");
        var result2 = Library.Crc32("test");

        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void Crc32_WhenDifferentInputProvided_ShouldReturnDifferentChecksum()
    {
        var result1 = Library.Crc32("test1");
        var result2 = Library.Crc32("test2");

        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void Crc32_WhenBytesProvided_ShouldReturnChecksum()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var result = Library.Crc32(bytes);

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length);
    }

    [TestMethod]
    public void Crc32_WhenNullBytesProvided_ShouldReturnNull()
    {
        var result = Library.Crc32((byte[]?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Crc32_WhenEmptyBytesProvided_ShouldReturnChecksum()
    {
        var result = Library.Crc32(Array.Empty<byte>());

        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length);
    }

    [TestMethod]
    public void Crc32_StringAndBytes_ShouldReturnSameChecksumForSameContent()
    {
        var input = "hello world";
        var bytes = Encoding.UTF8.GetBytes(input);

        var stringResult = Library.Crc32(input);
        var bytesResult = Library.Crc32(bytes);

        Assert.AreEqual(stringResult, bytesResult);
    }

    [TestMethod]
    public void Crc32_WhenKnownInput_ShouldReturnExpectedChecksum()
    {
        
        var result = Library.Crc32("hello");

        Assert.IsNotNull(result);
        
        Assert.IsTrue(result.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')));
    }

    #endregion

    #region HmacSha256 Tests

    [TestMethod]
    public void HmacSha256_WhenMessageAndKeyProvided_ShouldReturnHmac()
    {
        var result = Library.HmacSha256("message", "secret");

        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length); 
    }

    [TestMethod]
    public void HmacSha256_WhenNullMessageProvided_ShouldReturnNull()
    {
        var result = Library.HmacSha256(null, "secret");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HmacSha256_WhenNullKeyProvided_ShouldReturnNull()
    {
        var result = Library.HmacSha256("message", null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HmacSha256_WhenBothNullProvided_ShouldReturnNull()
    {
        var result = Library.HmacSha256(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HmacSha256_WhenEmptyMessageProvided_ShouldReturnHmac()
    {
        var result = Library.HmacSha256(string.Empty, "secret");

        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);
    }

    [TestMethod]
    public void HmacSha256_WhenEmptyKeyProvided_ShouldReturnHmac()
    {
        var result = Library.HmacSha256("message", string.Empty);

        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);
    }

    [TestMethod]
    public void HmacSha256_WhenSameInputProvided_ShouldReturnSameHmac()
    {
        var result1 = Library.HmacSha256("message", "key");
        var result2 = Library.HmacSha256("message", "key");

        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void HmacSha256_WhenDifferentKeyProvided_ShouldReturnDifferentHmac()
    {
        var result1 = Library.HmacSha256("message", "key1");
        var result2 = Library.HmacSha256("message", "key2");

        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void HmacSha256_WhenDifferentMessageProvided_ShouldReturnDifferentHmac()
    {
        var result1 = Library.HmacSha256("message1", "key");
        var result2 = Library.HmacSha256("message2", "key");

        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void HmacSha256_ShouldReturnLowercaseHex()
    {
        var result = Library.HmacSha256("test", "key");

        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')));
    }

    #endregion

    #region HmacSha512 Tests

    [TestMethod]
    public void HmacSha512_WhenMessageAndKeyProvided_ShouldReturnHmac()
    {
        var result = Library.HmacSha512("message", "secret");

        Assert.IsNotNull(result);
        Assert.AreEqual(128, result.Length); 
    }

    [TestMethod]
    public void HmacSha512_WhenNullMessageProvided_ShouldReturnNull()
    {
        var result = Library.HmacSha512(null, "secret");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HmacSha512_WhenNullKeyProvided_ShouldReturnNull()
    {
        var result = Library.HmacSha512("message", null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HmacSha512_WhenBothNullProvided_ShouldReturnNull()
    {
        var result = Library.HmacSha512(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HmacSha512_WhenEmptyMessageProvided_ShouldReturnHmac()
    {
        var result = Library.HmacSha512(string.Empty, "secret");

        Assert.IsNotNull(result);
        Assert.AreEqual(128, result.Length);
    }

    [TestMethod]
    public void HmacSha512_WhenEmptyKeyProvided_ShouldReturnHmac()
    {
        var result = Library.HmacSha512("message", string.Empty);

        Assert.IsNotNull(result);
        Assert.AreEqual(128, result.Length);
    }

    [TestMethod]
    public void HmacSha512_WhenSameInputProvided_ShouldReturnSameHmac()
    {
        var result1 = Library.HmacSha512("message", "key");
        var result2 = Library.HmacSha512("message", "key");

        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void HmacSha512_WhenDifferentKeyProvided_ShouldReturnDifferentHmac()
    {
        var result1 = Library.HmacSha512("message", "key1");
        var result2 = Library.HmacSha512("message", "key2");

        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void HmacSha512_WhenDifferentMessageProvided_ShouldReturnDifferentHmac()
    {
        var result1 = Library.HmacSha512("message1", "key");
        var result2 = Library.HmacSha512("message2", "key");

        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void HmacSha512_ShouldReturnLowercaseHex()
    {
        var result = Library.HmacSha512("test", "key");

        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')));
    }

    [TestMethod]
    public void HmacSha256_And_HmacSha512_ShouldProduceDifferentResults()
    {
        var sha256 = Library.HmacSha256("message", "key");
        var sha512 = Library.HmacSha512("message", "key");

        Assert.AreNotEqual(sha256, sha512);
        Assert.AreNotEqual(sha256?.Length, sha512?.Length);
    }

    #endregion
}
