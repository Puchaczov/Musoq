using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for cryptographic methods to improve branch coverage.
///     Tests SHA384, CRC32, HMAC methods.
/// </summary>
[TestClass]
public class CryptoExtendedTests : LibraryBaseBaseTests
{
    #region SHA384 Tests

    [TestMethod]
    public void Sha384_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.Sha384((string?)null));
    }

    [TestMethod]
    public void Sha384_ValidString_ReturnsHash()
    {
        var result = Library.Sha384("hello");
        Assert.IsNotNull(result);
        Assert.AreEqual(96, result.Length);
    }

    [TestMethod]
    public void Sha384_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.Sha384((byte[]?)null));
    }

    [TestMethod]
    public void Sha384_ValidBytes_ReturnsHash()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var result = Library.Sha384(bytes);
        Assert.IsNotNull(result);
        Assert.AreEqual(96, result.Length);
    }

    [TestMethod]
    public void Sha384_SameInput_ReturnsSameHash()
    {
        var hash1 = Library.Sha384("test");
        var hash2 = Library.Sha384("test");
        Assert.AreEqual(hash1, hash2);
    }

    #endregion

    #region CRC32 Tests

    [TestMethod]
    public void Crc32_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.Crc32((string?)null));
    }

    [TestMethod]
    public void Crc32_ValidString_ReturnsChecksum()
    {
        var result = Library.Crc32("hello");
        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length);
    }

    [TestMethod]
    public void Crc32_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.Crc32((byte[]?)null));
    }

    [TestMethod]
    public void Crc32_ValidBytes_ReturnsChecksum()
    {
        var bytes = Encoding.UTF8.GetBytes("hello");
        var result = Library.Crc32(bytes);
        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length);
    }

    [TestMethod]
    public void Crc32_SameInput_ReturnsSameChecksum()
    {
        var crc1 = Library.Crc32("test");
        var crc2 = Library.Crc32("test");
        Assert.AreEqual(crc1, crc2);
    }

    #endregion

    #region HMAC Tests

    [TestMethod]
    public void HmacSha256_NullMessage_ReturnsNull()
    {
        Assert.IsNull(Library.HmacSha256(null, "key"));
    }

    [TestMethod]
    public void HmacSha256_NullKey_ReturnsNull()
    {
        Assert.IsNull(Library.HmacSha256("message", null));
    }

    [TestMethod]
    public void HmacSha256_ValidInputs_ReturnsHmac()
    {
        var result = Library.HmacSha256("message", "key");
        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);
    }

    [TestMethod]
    public void HmacSha256_SameInputs_ReturnsSameHmac()
    {
        var hmac1 = Library.HmacSha256("message", "key");
        var hmac2 = Library.HmacSha256("message", "key");
        Assert.AreEqual(hmac1, hmac2);
    }

    [TestMethod]
    public void HmacSha512_NullMessage_ReturnsNull()
    {
        Assert.IsNull(Library.HmacSha512(null, "key"));
    }

    [TestMethod]
    public void HmacSha512_NullKey_ReturnsNull()
    {
        Assert.IsNull(Library.HmacSha512("message", null));
    }

    [TestMethod]
    public void HmacSha512_ValidInputs_ReturnsHmac()
    {
        var result = Library.HmacSha512("message", "key");
        Assert.IsNotNull(result);
        Assert.AreEqual(128, result.Length);
    }

    [TestMethod]
    public void HmacSha512_SameInputs_ReturnsSameHmac()
    {
        var hmac1 = Library.HmacSha512("message", "key");
        var hmac2 = Library.HmacSha512("message", "key");
        Assert.AreEqual(hmac1, hmac2);
    }

    #endregion
}
