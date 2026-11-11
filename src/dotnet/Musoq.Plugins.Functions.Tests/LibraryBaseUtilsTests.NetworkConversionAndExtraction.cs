using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseUtilsTests
{
    #region IP Utilities Tests

    [TestMethod]
    public void IsPrivateIP_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsPrivateIp(null));
    }

    [TestMethod]
    public void IsPrivateIP_WhenPrivate_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsPrivateIp("10.0.0.1"));
        Assert.IsTrue(_library.IsPrivateIp("172.16.0.1"));
        Assert.IsTrue(_library.IsPrivateIp("192.168.1.1"));
        Assert.IsTrue(_library.IsPrivateIp("127.0.0.1"));
    }

    [TestMethod]
    public void IsPrivateIP_WhenPublic_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsPrivateIp("8.8.8.8"));
        Assert.IsFalse(_library.IsPrivateIp("1.1.1.1"));
    }

    [TestMethod]
    public void IpToLong_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IpToLong(null));
    }

    [TestMethod]
    public void IpToLong_WhenValid_ReturnsNumber()
    {
        Assert.AreEqual(3232235777L, _library.IpToLong("192.168.1.1"));
    }

    [TestMethod]
    public void LongToIp_WhenValid_ReturnsIp()
    {
        Assert.AreEqual("192.168.1.1", _library.LongToIp(3232235777L));
    }

    [TestMethod]
    public void IsInSubnet_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsInSubnet(null, "192.168.1.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_WhenInSubnet_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsInSubnet("192.168.1.100", "192.168.1.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_WhenNotInSubnet_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsInSubnet("192.168.2.1", "192.168.1.0/24"));
    }

    [TestMethod]
    public void FormatMac_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FormatMac(null));
    }

    [TestMethod]
    public void FormatMac_WhenValid_FormatsCorrectly()
    {
        Assert.AreEqual("AA:BB:CC:DD:EE:FF", _library.FormatMac("aabbccddeeff"));
        Assert.AreEqual("AA-BB-CC-DD-EE-FF", _library.FormatMac("AA:BB:CC:DD:EE:FF", "-"));
    }

    #endregion

    #region Conversion Tests

    [TestMethod]
    public void ConvertBase_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ConvertBase(null, 10, 2));
    }

    [TestMethod]
    public void ConvertBase_DecimalToBinary()
    {
        Assert.AreEqual("1010", _library.ConvertBase("10", 10, 2));
    }

    [TestMethod]
    public void ConvertBase_HexToDecimal()
    {
        Assert.AreEqual("255", _library.ConvertBase("FF", 16, 10));
    }

    [TestMethod]
    public void UnixToDateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.UnixToDateTime(null));
    }

    [TestMethod]
    public void UnixToDateTime_WhenValid_ReturnsDateTime()
    {
        var dt = _library.UnixToDateTime(0);
        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), dt);
    }

    [TestMethod]
    public void DateTimeToUnix_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.DateTimeToUnix(null));
    }

    [TestMethod]
    public void DateTimeToUnix_RoundTrips()
    {
        var original = 1700000000L;
        var dt = _library.UnixToDateTime(original);
        var back = _library.DateTimeToUnix(dt);
        Assert.AreEqual(original, back);
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.UnixMillisToDateTime(null));
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenValid_ReturnsDateTime()
    {
        var dt = _library.UnixMillisToDateTime(0);
        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), dt);
    }

    [TestMethod]
    public void DateTimeToUnixMillis_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.DateTimeToUnixMillis(null));
    }

    [TestMethod]
    public void DateTimeToUnixMillis_RoundTrips()
    {
        var original = 1700000000000L;
        var dt = _library.UnixMillisToDateTime(original);
        var back = _library.DateTimeToUnixMillis(dt);
        Assert.AreEqual(original, back);
    }

    #endregion

    #region Slug and Escape Tests

    [TestMethod]
    public void ToSlug_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToSlug(null));
    }

    [TestMethod]
    public void ToSlug_WhenValid_ReturnsSlug()
    {
        Assert.AreEqual("hello-world", _library.ToSlug("Hello World!"));
        Assert.AreEqual("cafe-au-lait", _library.ToSlug("Café au Lait"));
    }

    [TestMethod]
    public void EscapeRegex_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.EscapeRegex(null));
    }

    [TestMethod]
    public void EscapeRegex_WhenSpecialChars_EscapesThem()
    {
        Assert.AreEqual(@"\[test]", _library.EscapeRegex("[test]"));
        Assert.AreEqual(@"a\.b", _library.EscapeRegex("a.b"));
    }

    [TestMethod]
    public void EscapeSql_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.EscapeSql(null));
    }

    [TestMethod]
    public void EscapeSql_WhenSingleQuotes_DoublesThem()
    {
        Assert.AreEqual("O''Brien", _library.EscapeSql("O'Brien"));
    }

    #endregion

    #region Extraction Tests

    [TestMethod]
    public void ExtractUrls_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ExtractUrls(null));
    }

    [TestMethod]
    public void ExtractUrls_WhenContainsUrls_ExtractsThem()
    {
        var result = _library.ExtractUrls("Visit https://example.com and http://test.org");
        Assert.IsNotNull(result);
        Assert.Contains("https://example.com", result);
        Assert.Contains("http://test.org", result);
    }

    [TestMethod]
    public void ExtractEmails_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ExtractEmails(null));
    }

    [TestMethod]
    public void ExtractEmails_WhenContainsEmails_ExtractsThem()
    {
        var result = _library.ExtractEmails("Contact: test@example.com or admin@test.org");
        Assert.IsNotNull(result);
        Assert.Contains("test@example.com", result);
        Assert.Contains("admin@test.org", result);
    }

    [TestMethod]
    public void ExtractIPs_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ExtractIPs(null));
    }

    [TestMethod]
    public void ExtractIPs_WhenContainsIPs_ExtractsThem()
    {
        var result = _library.ExtractIPs("Server at 192.168.1.1 and 10.0.0.1");
        Assert.IsNotNull(result);
        Assert.Contains("192.168.1.1", result);
        Assert.Contains("10.0.0.1", result);
    }

    [TestMethod]
    public void NewGuid_ReturnsValidGuid()
    {
        var guid = _library.NewGuid();
        Assert.IsTrue(Guid.TryParse(guid, out _));
    }

    [TestMethod]
    public void NewGuidCompact_ReturnsGuidWithoutDashes()
    {
        var guid = _library.NewGuidCompact();
        Assert.AreEqual(32, guid.Length);
        Assert.DoesNotContain('-', guid);
    }

    #endregion
}
