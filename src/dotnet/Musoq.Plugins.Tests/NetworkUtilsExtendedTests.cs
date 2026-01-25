using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for network utility methods to improve branch coverage.
///     Tests IsPrivateIP, IpToLong, LongToIp, IsInSubnet, FormatMac, ConvertBase,
///     Unix timestamps, ToSlug, EscapeRegex, EscapeSql, ExtractUrls, ExtractEmails, ExtractIPs.
/// </summary>
[TestClass]
public class NetworkUtilsExtendedTests : LibraryBaseBaseTests
{
    #region IsPrivateIP Tests

    [TestMethod]
    public void IsPrivateIP_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsPrivateIP(null));
    }

    [TestMethod]
    public void IsPrivateIP_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.IsPrivateIP(string.Empty));
    }

    [TestMethod]
    public void IsPrivateIP_InvalidIP_ReturnsNull()
    {
        Assert.IsNull(Library.IsPrivateIP("not an ip"));
    }

    [TestMethod]
    public void IsPrivateIP_10Network_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIP("10.0.0.1"));
        Assert.IsTrue(Library.IsPrivateIP("10.255.255.255"));
    }

    [TestMethod]
    public void IsPrivateIP_172Network_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIP("172.16.0.1"));
        Assert.IsTrue(Library.IsPrivateIP("172.31.255.255"));
    }

    [TestMethod]
    public void IsPrivateIP_172NetworkOutsideRange_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsPrivateIP("172.15.0.1"));
        Assert.IsFalse(Library.IsPrivateIP("172.32.0.1"));
    }

    [TestMethod]
    public void IsPrivateIP_192168Network_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIP("192.168.0.1"));
        Assert.IsTrue(Library.IsPrivateIP("192.168.255.255"));
    }

    [TestMethod]
    public void IsPrivateIP_Localhost_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIP("127.0.0.1"));
    }

    [TestMethod]
    public void IsPrivateIP_PublicIP_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsPrivateIP("8.8.8.8"));
        Assert.IsFalse(Library.IsPrivateIP("1.1.1.1"));
    }

    [TestMethod]
    public void IsPrivateIP_IPv6_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsPrivateIP("::1"));
    }

    #endregion

    #region IpToLong Tests

    [TestMethod]
    public void IpToLong_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IpToLong(null));
    }

    [TestMethod]
    public void IpToLong_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.IpToLong(string.Empty));
    }

    [TestMethod]
    public void IpToLong_InvalidIP_ReturnsNull()
    {
        Assert.IsNull(Library.IpToLong("not an ip"));
    }

    [TestMethod]
    public void IpToLong_ValidIP_ReturnsCorrectLong()
    {
        Assert.AreEqual(3232235521L, Library.IpToLong("192.168.0.1"));
    }

    [TestMethod]
    public void IpToLong_ZeroIP_ReturnsZero()
    {
        Assert.AreEqual(0L, Library.IpToLong("0.0.0.0"));
    }

    [TestMethod]
    public void IpToLong_MaxIP_ReturnsMaxValue()
    {
        Assert.AreEqual(4294967295L, Library.IpToLong("255.255.255.255"));
    }

    [TestMethod]
    public void IpToLong_IPv6_ReturnsNull()
    {
        Assert.IsNull(Library.IpToLong("::1"));
    }

    #endregion

    #region LongToIp Tests

    [TestMethod]
    public void LongToIp_Null_ReturnsNull()
    {
        Assert.IsNull(Library.LongToIp(null));
    }

    [TestMethod]
    public void LongToIp_Negative_ReturnsNull()
    {
        Assert.IsNull(Library.LongToIp(-1));
    }

    [TestMethod]
    public void LongToIp_TooLarge_ReturnsNull()
    {
        Assert.IsNull(Library.LongToIp((long)uint.MaxValue + 1));
    }

    [TestMethod]
    public void LongToIp_Zero_ReturnsZeroIP()
    {
        Assert.AreEqual("0.0.0.0", Library.LongToIp(0));
    }

    [TestMethod]
    public void LongToIp_ValidLong_ReturnsCorrectIP()
    {
        Assert.AreEqual("192.168.0.1", Library.LongToIp(3232235521L));
    }

    [TestMethod]
    public void LongToIp_MaxValue_ReturnsMaxIP()
    {
        Assert.AreEqual("255.255.255.255", Library.LongToIp(4294967295L));
    }

    #endregion

    #region IsInSubnet Tests

    [TestMethod]
    public void IsInSubnet_NullIP_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet(null, "192.168.0.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_NullCidr_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("192.168.0.1", null));
    }

    [TestMethod]
    public void IsInSubnet_EmptyIP_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet(string.Empty, "192.168.0.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_EmptyCidr_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("192.168.0.1", string.Empty));
    }

    [TestMethod]
    public void IsInSubnet_InvalidCidrFormat_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("192.168.0.1", "192.168.0.0"));
    }

    [TestMethod]
    public void IsInSubnet_InvalidIP_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("not an ip", "192.168.0.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_InvalidSubnetIP_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("192.168.0.1", "not.an.ip/24"));
    }

    [TestMethod]
    public void IsInSubnet_InvalidPrefixLength_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("192.168.0.1", "192.168.0.0/abc"));
    }

    [TestMethod]
    public void IsInSubnet_PrefixLengthNegative_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("192.168.0.1", "192.168.0.0/-1"));
    }

    [TestMethod]
    public void IsInSubnet_PrefixLengthTooLarge_ReturnsNull()
    {
        Assert.IsNull(Library.IsInSubnet("192.168.0.1", "192.168.0.0/33"));
    }

    [TestMethod]
    public void IsInSubnet_InSubnet_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsInSubnet("192.168.0.100", "192.168.0.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_NotInSubnet_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsInSubnet("192.168.1.100", "192.168.0.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_PrefixZero_AllIPsMatch()
    {
        Assert.IsTrue(Library.IsInSubnet("1.2.3.4", "192.168.0.0/0"));
    }

    #endregion

    #region FormatMac Tests

    [TestMethod]
    public void FormatMac_Null_ReturnsNull()
    {
        Assert.IsNull(Library.FormatMac(null));
    }

    [TestMethod]
    public void FormatMac_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FormatMac(string.Empty));
    }

    [TestMethod]
    public void FormatMac_ValidMacWithColons_FormatsCorrectly()
    {
        Assert.AreEqual("00:11:22:33:44:55", Library.FormatMac("00:11:22:33:44:55"));
    }

    [TestMethod]
    public void FormatMac_ValidMacWithDashes_FormatsCorrectly()
    {
        Assert.AreEqual("00:11:22:33:44:55", Library.FormatMac("00-11-22-33-44-55"));
    }

    [TestMethod]
    public void FormatMac_ValidMacWithoutSeparators_FormatsCorrectly()
    {
        Assert.AreEqual("00:11:22:33:44:55", Library.FormatMac("001122334455"));
    }

    [TestMethod]
    public void FormatMac_CustomSeparator_FormatsCorrectly()
    {
        Assert.AreEqual("00-11-22-33-44-55", Library.FormatMac("001122334455", "-"));
    }

    [TestMethod]
    public void FormatMac_InvalidLength_ReturnsNull()
    {
        Assert.IsNull(Library.FormatMac("001122334"));
    }

    [TestMethod]
    public void FormatMac_LowercaseInput_ReturnsUppercase()
    {
        Assert.AreEqual("AA:BB:CC:DD:EE:FF", Library.FormatMac("aabbccddeeff"));
    }

    #endregion

    #region ConvertBase Tests

    [TestMethod]
    public void ConvertBase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ConvertBase(null, 10, 2));
    }

    [TestMethod]
    public void ConvertBase_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ConvertBase(string.Empty, 10, 2));
    }

    [TestMethod]
    public void ConvertBase_InvalidFromBase_ReturnsNull()
    {
        Assert.IsNull(Library.ConvertBase("10", 1, 10));
        Assert.IsNull(Library.ConvertBase("10", 37, 10));
    }

    [TestMethod]
    public void ConvertBase_InvalidToBase_ReturnsNull()
    {
        Assert.IsNull(Library.ConvertBase("10", 10, 1));
        Assert.IsNull(Library.ConvertBase("10", 10, 37));
    }

    [TestMethod]
    public void ConvertBase_DecimalToBinary_Converts()
    {
        Assert.AreEqual("1010", Library.ConvertBase("10", 10, 2));
    }

    [TestMethod]
    public void ConvertBase_BinaryToDecimal_Converts()
    {
        Assert.AreEqual("10", Library.ConvertBase("1010", 2, 10));
    }

    [TestMethod]
    public void ConvertBase_DecimalToHex_Converts()
    {
        Assert.AreEqual("FF", Library.ConvertBase("255", 10, 16));
    }

    [TestMethod]
    public void ConvertBase_HexToDecimal_Converts()
    {
        Assert.AreEqual("255", Library.ConvertBase("FF", 16, 10));
    }

    [TestMethod]
    public void ConvertBase_Zero_ReturnsZero()
    {
        Assert.AreEqual("0", Library.ConvertBase("0", 10, 2));
    }

    [TestMethod]
    public void ConvertBase_InvalidNumber_ReturnsNull()
    {
        Assert.IsNull(Library.ConvertBase("not a number", 10, 2));
    }

    #endregion

    #region Unix Timestamp Tests

    [TestMethod]
    public void UnixToDateTime_Null_ReturnsNull()
    {
        Assert.IsNull(Library.UnixToDateTime(null));
    }

    [TestMethod]
    public void UnixToDateTime_Zero_Returns1970()
    {
        var result = Library.UnixToDateTime(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1970, result.Value.Year);
        Assert.AreEqual(1, result.Value.Month);
        Assert.AreEqual(1, result.Value.Day);
    }

    [TestMethod]
    public void UnixToDateTime_ValidTimestamp_ReturnsCorrectDate()
    {
        var result = Library.UnixToDateTime(1609459200);
        Assert.IsNotNull(result);
        Assert.AreEqual(2021, result.Value.Year);
    }

    [TestMethod]
    public void UnixMillisToDateTime_Null_ReturnsNull()
    {
        Assert.IsNull(Library.UnixMillisToDateTime(null));
    }

    [TestMethod]
    public void UnixMillisToDateTime_Zero_Returns1970()
    {
        var result = Library.UnixMillisToDateTime(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1970, result.Value.Year);
    }

    [TestMethod]
    public void DateTimeToUnix_Null_ReturnsNull()
    {
        Assert.IsNull(Library.DateTimeToUnix(null));
    }

    [TestMethod]
    public void DateTimeToUnix_ValidDateTime_ReturnsTimestamp()
    {
        var result = Library.DateTimeToUnix(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public void DateTimeToUnixMillis_Null_ReturnsNull()
    {
        Assert.IsNull(Library.DateTimeToUnixMillis(null));
    }

    [TestMethod]
    public void DateTimeToUnixMillis_ValidDateTime_ReturnsTimestamp()
    {
        var result = Library.DateTimeToUnixMillis(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.AreEqual(0L, result);
    }

    #endregion

    #region ToSlug Tests

    [TestMethod]
    public void ToSlug_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToSlug(null));
    }

    [TestMethod]
    public void ToSlug_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ToSlug(string.Empty));
    }

    [TestMethod]
    public void ToSlug_SimpleString_ReturnsLowercase()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello World"));
    }

    [TestMethod]
    public void ToSlug_WithSpecialChars_RemovesThem()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello! World?"));
    }

    [TestMethod]
    public void ToSlug_WithDashes_PreservesThemAsSlug()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello-World"));
    }

    [TestMethod]
    public void ToSlug_WithUnderscores_ConvertsToDashes()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello_World"));
    }

    [TestMethod]
    public void ToSlug_WithMultipleSpaces_UseSingleDash()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello   World"));
    }

    [TestMethod]
    public void ToSlug_WithAccents_RemovesAccents()
    {
        Assert.AreEqual("cafe", Library.ToSlug("Café"));
    }

    #endregion

    #region EscapeRegex Tests

    [TestMethod]
    public void EscapeRegex_Null_ReturnsNull()
    {
        Assert.IsNull(Library.EscapeRegex(null));
    }

    [TestMethod]
    public void EscapeRegex_NormalString_ReturnsUnchanged()
    {
        Assert.AreEqual("hello", Library.EscapeRegex("hello"));
    }

    [TestMethod]
    public void EscapeRegex_SpecialChars_EscapesThem()
    {
        Assert.AreEqual(@"\[test]", Library.EscapeRegex("[test]"));
    }

    #endregion

    #region EscapeSql Tests

    [TestMethod]
    public void EscapeSql_Null_ReturnsNull()
    {
        Assert.IsNull(Library.EscapeSql(null));
    }

    [TestMethod]
    public void EscapeSql_NoQuotes_ReturnsUnchanged()
    {
        Assert.AreEqual("hello", Library.EscapeSql("hello"));
    }

    [TestMethod]
    public void EscapeSql_SingleQuote_Doubled()
    {
        Assert.AreEqual("it''s", Library.EscapeSql("it's"));
    }

    [TestMethod]
    public void EscapeSql_MultipleQuotes_AllDoubled()
    {
        Assert.AreEqual("''test''", Library.EscapeSql("'test'"));
    }

    #endregion

    #region ExtractUrls Tests

    [TestMethod]
    public void ExtractUrls_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractUrls(null));
    }

    [TestMethod]
    public void ExtractUrls_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractUrls(string.Empty));
    }

    [TestMethod]
    public void ExtractUrls_NoUrls_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ExtractUrls("no urls here"));
    }

    [TestMethod]
    public void ExtractUrls_SingleUrl_ReturnsUrl()
    {
        Assert.AreEqual("https://example.com", Library.ExtractUrls("Visit https://example.com today"));
    }

    [TestMethod]
    public void ExtractUrls_MultipleUrls_ReturnsCommaSeparated()
    {
        var result = Library.ExtractUrls("Visit https://a.com and http://b.com");
        Assert.IsNotNull(result);
        Assert.Contains("https://a.com", result);
        Assert.Contains("http://b.com", result);
    }

    #endregion

    #region ExtractEmails Tests

    [TestMethod]
    public void ExtractEmails_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractEmails(null));
    }

    [TestMethod]
    public void ExtractEmails_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractEmails(string.Empty));
    }

    [TestMethod]
    public void ExtractEmails_NoEmails_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ExtractEmails("no emails here"));
    }

    [TestMethod]
    public void ExtractEmails_SingleEmail_ReturnsEmail()
    {
        Assert.AreEqual("test@example.com", Library.ExtractEmails("Contact test@example.com"));
    }

    [TestMethod]
    public void ExtractEmails_MultipleEmails_ReturnsCommaSeparated()
    {
        var result = Library.ExtractEmails("Contact a@b.com or c@d.com");
        Assert.IsNotNull(result);
        Assert.Contains("a@b.com", result);
        Assert.Contains("c@d.com", result);
    }

    #endregion

    #region ExtractIPs Tests

    [TestMethod]
    public void ExtractIPs_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractIPs(null));
    }

    [TestMethod]
    public void ExtractIPs_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractIPs(string.Empty));
    }

    [TestMethod]
    public void ExtractIPs_NoIPs_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ExtractIPs("no ips here"));
    }

    [TestMethod]
    public void ExtractIPs_SingleIP_ReturnsIP()
    {
        Assert.AreEqual("192.168.0.1", Library.ExtractIPs("Server at 192.168.0.1"));
    }

    [TestMethod]
    public void ExtractIPs_MultipleIPs_ReturnsCommaSeparated()
    {
        var result = Library.ExtractIPs("Servers 192.168.0.1 and 10.0.0.1");
        Assert.IsNotNull(result);
        Assert.Contains("192.168.0.1", result);
        Assert.Contains("10.0.0.1", result);
    }

    #endregion

    #region NewGuid Tests

    [TestMethod]
    public void NewGuid_ReturnsValidGuid()
    {
        var result = Library.NewGuid();
        Assert.IsTrue(Guid.TryParse(result, out _));
    }

    [TestMethod]
    public void NewGuid_ReturnsUnique()
    {
        var guid1 = Library.NewGuid();
        var guid2 = Library.NewGuid();
        Assert.AreNotEqual(guid1, guid2);
    }

    [TestMethod]
    public void NewGuidCompact_ReturnsGuidWithoutDashes()
    {
        var result = Library.NewGuidCompact();
        Assert.DoesNotContain("-", result);
        Assert.AreEqual(32, result.Length);
    }

    #endregion
}
