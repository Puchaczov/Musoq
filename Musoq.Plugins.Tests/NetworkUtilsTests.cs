using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class NetworkUtilsTests : LibraryBaseBaseTests
{
    #region IsPrivateIP Tests

    [TestMethod]
    public void IsPrivateIP_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsPrivateIP(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenEmptyStringProvided_ShouldReturnNull()
    {
        var result = Library.IsPrivateIP(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenInvalidIPProvided_ShouldReturnNull()
    {
        var result = Library.IsPrivateIP("not-an-ip");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsPrivateIP_When10NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("10.0.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When10NetworkMaxProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("10.255.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_16NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("172.16.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_31NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("172.31.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_15NetworkProvided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIP("172.15.0.1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_32NetworkProvided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIP("172.32.0.1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrivateIP_When192_168NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("192.168.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When192_168MaxProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("192.168.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenLocalhostProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("127.0.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When127NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIP("127.255.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenPublicIPProvided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIP("8.8.8.8");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenIPv6Provided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIP("::1");

        Assert.IsFalse(result);
    }

    #endregion

    #region IpToLong Tests

    [TestMethod]
    public void IpToLong_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IpToLong(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IpToLong_WhenEmptyStringProvided_ShouldReturnNull()
    {
        var result = Library.IpToLong(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IpToLong_WhenInvalidIPProvided_ShouldReturnNull()
    {
        var result = Library.IpToLong("not-an-ip");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IpToLong_When0_0_0_0Provided_ShouldReturn0()
    {
        var result = Library.IpToLong("0.0.0.0");

        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public void IpToLong_When255_255_255_255Provided_ShouldReturnMax()
    {
        var result = Library.IpToLong("255.255.255.255");

        Assert.AreEqual(4294967295L, result);
    }

    [TestMethod]
    public void IpToLong_When192_168_1_1Provided_ShouldReturnCorrectValue()
    {
        var result = Library.IpToLong("192.168.1.1");

        // 192*2^24 + 168*2^16 + 1*2^8 + 1 = 3232235777
        Assert.AreEqual(3232235777L, result);
    }

    [TestMethod]
    public void IpToLong_When10_0_0_1Provided_ShouldReturnCorrectValue()
    {
        var result = Library.IpToLong("10.0.0.1");

        // 10*2^24 + 0 + 0 + 1 = 167772161
        Assert.AreEqual(167772161L, result);
    }

    [TestMethod]
    public void IpToLong_WhenIPv6Provided_ShouldReturnNull()
    {
        var result = Library.IpToLong("::1");

        Assert.IsNull(result);
    }

    #endregion

    #region LongToIp Tests

    [TestMethod]
    public void LongToIp_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.LongToIp(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void LongToIp_WhenNegativeProvided_ShouldReturnNull()
    {
        var result = Library.LongToIp(-1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void LongToIp_WhenTooLargeProvided_ShouldReturnNull()
    {
        var result = Library.LongToIp((long)uint.MaxValue + 1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void LongToIp_When0Provided_ShouldReturn0_0_0_0()
    {
        var result = Library.LongToIp(0);

        Assert.AreEqual("0.0.0.0", result);
    }

    [TestMethod]
    public void LongToIp_WhenMaxProvided_ShouldReturn255_255_255_255()
    {
        var result = Library.LongToIp(4294967295L);

        Assert.AreEqual("255.255.255.255", result);
    }

    [TestMethod]
    public void LongToIp_When3232235777Provided_ShouldReturn192_168_1_1()
    {
        var result = Library.LongToIp(3232235777L);

        Assert.AreEqual("192.168.1.1", result);
    }

    [TestMethod]
    public void IpToLong_And_LongToIp_ShouldBeReversible()
    {
        var originalIp = "192.168.100.50";
        var longValue = Library.IpToLong(originalIp);
        var resultIp = Library.LongToIp(longValue);

        Assert.AreEqual(originalIp, resultIp);
    }

    #endregion

    #region IsInSubnet Tests

    [TestMethod]
    public void IsInSubnet_WhenNullIPProvided_ShouldReturnNull()
    {
        var result = Library.IsInSubnet(null, "192.168.1.0/24");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenNullCIDRProvided_ShouldReturnNull()
    {
        var result = Library.IsInSubnet("192.168.1.1", null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenEmptyIPProvided_ShouldReturnNull()
    {
        var result = Library.IsInSubnet(string.Empty, "192.168.1.0/24");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenEmptyCIDRProvided_ShouldReturnNull()
    {
        var result = Library.IsInSubnet("192.168.1.1", string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenInvalidCIDRFormat_ShouldReturnNull()
    {
        var result = Library.IsInSubnet("192.168.1.1", "192.168.1.0");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenIPInSubnet_ShouldReturnTrue()
    {
        var result = Library.IsInSubnet("192.168.1.100", "192.168.1.0/24");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenIPNotInSubnet_ShouldReturnFalse()
    {
        var result = Library.IsInSubnet("192.168.2.100", "192.168.1.0/24");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenLargeSubnet_ShouldWork()
    {
        var result = Library.IsInSubnet("10.50.100.200", "10.0.0.0/8");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenSmallSubnet_ShouldWork()
    {
        var result1 = Library.IsInSubnet("192.168.1.0", "192.168.1.0/32");
        var result2 = Library.IsInSubnet("192.168.1.1", "192.168.1.0/32");

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [TestMethod]
    public void IsInSubnet_WhenZeroPrefixLength_ShouldMatchAll()
    {
        var result = Library.IsInSubnet("8.8.8.8", "0.0.0.0/0");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenInvalidPrefixLength_ShouldReturnNull()
    {
        var result = Library.IsInSubnet("192.168.1.1", "192.168.1.0/33");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsInSubnet_WhenNegativePrefixLength_ShouldReturnNull()
    {
        var result = Library.IsInSubnet("192.168.1.1", "192.168.1.0/-1");

        Assert.IsNull(result);
    }

    #endregion

    #region FormatMac Tests

    [TestMethod]
    public void FormatMac_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.FormatMac(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FormatMac_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.FormatMac(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FormatMac_WhenValidMacWithColons_ShouldFormat()
    {
        var result = Library.FormatMac("aa:bb:cc:dd:ee:ff");

        Assert.AreEqual("AA:BB:CC:DD:EE:FF", result);
    }

    [TestMethod]
    public void FormatMac_WhenValidMacWithDashes_ShouldFormat()
    {
        var result = Library.FormatMac("AA-BB-CC-DD-EE-FF");

        Assert.AreEqual("AA:BB:CC:DD:EE:FF", result);
    }

    [TestMethod]
    public void FormatMac_WhenValidMacWithoutSeparators_ShouldFormat()
    {
        var result = Library.FormatMac("AABBCCDDEEFF");

        Assert.AreEqual("AA:BB:CC:DD:EE:FF", result);
    }

    [TestMethod]
    public void FormatMac_WhenCustomSeparatorProvided_ShouldUseIt()
    {
        var result = Library.FormatMac("AABBCCDDEEFF", "-");

        Assert.AreEqual("AA-BB-CC-DD-EE-FF", result);
    }

    [TestMethod]
    public void FormatMac_WhenInvalidLengthProvided_ShouldReturnNull()
    {
        var result = Library.FormatMac("AABBCCDD");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FormatMac_WhenTooLongProvided_ShouldReturnNull()
    {
        var result = Library.FormatMac("AABBCCDDEEFF00");

        Assert.IsNull(result);
    }

    #endregion

    #region NewGuid Tests

    [TestMethod]
    public void NewGuid_ShouldReturnValidGuid()
    {
        var result = Library.NewGuid();

        Assert.IsNotNull(result);
        Assert.IsTrue(Guid.TryParse(result, out _));
    }

    [TestMethod]
    public void NewGuid_ShouldContainDashes()
    {
        var result = Library.NewGuid();

        Assert.Contains('-', result);
        Assert.AreEqual(36, result.Length);
    }

    [TestMethod]
    public void NewGuid_MultipleCalls_ShouldReturnDifferentGuids()
    {
        var result1 = Library.NewGuid();
        var result2 = Library.NewGuid();

        Assert.AreNotEqual(result1, result2);
    }

    #endregion

    #region NewGuidCompact Tests

    [TestMethod]
    public void NewGuidCompact_ShouldReturnValidGuid()
    {
        var result = Library.NewGuidCompact();

        Assert.IsNotNull(result);
        Assert.IsTrue(Guid.TryParse(result, out _));
    }

    [TestMethod]
    public void NewGuidCompact_ShouldNotContainDashes()
    {
        var result = Library.NewGuidCompact();

        Assert.DoesNotContain('-', result);
        Assert.AreEqual(32, result.Length);
    }

    [TestMethod]
    public void NewGuidCompact_MultipleCalls_ShouldReturnDifferentGuids()
    {
        var result1 = Library.NewGuidCompact();
        var result2 = Library.NewGuidCompact();

        Assert.AreNotEqual(result1, result2);
    }

    #endregion

    #region ConvertBase Tests

    [TestMethod]
    public void ConvertBase_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ConvertBase(null, 10, 2);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ConvertBase_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ConvertBase(string.Empty, 10, 2);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ConvertBase_WhenDecimalToBinary_ShouldConvert()
    {
        var result = Library.ConvertBase("10", 10, 2);

        Assert.AreEqual("1010", result);
    }

    [TestMethod]
    public void ConvertBase_WhenBinaryToDecimal_ShouldConvert()
    {
        var result = Library.ConvertBase("1010", 2, 10);

        Assert.AreEqual("10", result);
    }

    [TestMethod]
    public void ConvertBase_WhenDecimalToHex_ShouldConvert()
    {
        var result = Library.ConvertBase("255", 10, 16);

        Assert.AreEqual("FF", result);
    }

    [TestMethod]
    public void ConvertBase_WhenHexToDecimal_ShouldConvert()
    {
        var result = Library.ConvertBase("FF", 16, 10);

        Assert.AreEqual("255", result);
    }

    [TestMethod]
    public void ConvertBase_WhenInvalidFromBase_ShouldReturnNull()
    {
        var result = Library.ConvertBase("10", 1, 10);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ConvertBase_WhenInvalidToBase_ShouldReturnNull()
    {
        var result = Library.ConvertBase("10", 10, 37);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ConvertBase_WhenZeroProvided_ShouldReturnZero()
    {
        var result = Library.ConvertBase("0", 10, 2);

        Assert.AreEqual("0", result);
    }

    #endregion

    #region UnixToDateTime Tests

    [TestMethod]
    public void UnixToDateTime_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.UnixToDateTime(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void UnixToDateTime_WhenZeroProvided_ShouldReturnEpoch()
    {
        var result = Library.UnixToDateTime(0);

        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void UnixToDateTime_WhenValidTimestampProvided_ShouldConvert()
    {
        // 2024-01-01 00:00:00 UTC
        var result = Library.UnixToDateTime(1704067200);

        Assert.AreEqual(2024, result?.Year);
        Assert.AreEqual(1, result?.Month);
        Assert.AreEqual(1, result?.Day);
    }

    #endregion

    #region UnixMillisToDateTime Tests

    [TestMethod]
    public void UnixMillisToDateTime_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.UnixMillisToDateTime(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenZeroProvided_ShouldReturnEpoch()
    {
        var result = Library.UnixMillisToDateTime(0);

        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenValidTimestampProvided_ShouldConvert()
    {
        // 2024-01-01 00:00:00.000 UTC
        var result = Library.UnixMillisToDateTime(1704067200000);

        Assert.AreEqual(2024, result?.Year);
        Assert.AreEqual(1, result?.Month);
        Assert.AreEqual(1, result?.Day);
    }

    #endregion

    #region DateTimeToUnix Tests

    [TestMethod]
    public void DateTimeToUnix_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.DateTimeToUnix(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void DateTimeToUnix_WhenEpochProvided_ShouldReturnZero()
    {
        var result = Library.DateTimeToUnix(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public void DateTimeToUnix_And_UnixToDateTime_ShouldBeReversible()
    {
        var original = new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var unix = Library.DateTimeToUnix(original);
        var result = Library.UnixToDateTime(unix);

        Assert.AreEqual(original.Year, result?.Year);
        Assert.AreEqual(original.Month, result?.Month);
        Assert.AreEqual(original.Day, result?.Day);
        Assert.AreEqual(original.Hour, result?.Hour);
        Assert.AreEqual(original.Minute, result?.Minute);
        Assert.AreEqual(original.Second, result?.Second);
    }

    #endregion

    #region DateTimeToUnixMillis Tests

    [TestMethod]
    public void DateTimeToUnixMillis_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.DateTimeToUnixMillis(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void DateTimeToUnixMillis_WhenEpochProvided_ShouldReturnZero()
    {
        var result = Library.DateTimeToUnixMillis(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(0L, result);
    }

    #endregion

    #region ToSlug Tests

    [TestMethod]
    public void ToSlug_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ToSlug(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToSlug_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ToSlug(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToSlug_WhenSimpleStringProvided_ShouldConvert()
    {
        var result = Library.ToSlug("Hello World");

        Assert.AreEqual("hello-world", result);
    }

    [TestMethod]
    public void ToSlug_WhenSpecialCharsProvided_ShouldRemove()
    {
        var result = Library.ToSlug("Hello! World?");

        Assert.AreEqual("hello-world", result);
    }

    [TestMethod]
    public void ToSlug_WhenAccentsProvided_ShouldNormalize()
    {
        var result = Library.ToSlug("Café résumé");

        Assert.AreEqual("cafe-resume", result);
    }

    [TestMethod]
    public void ToSlug_WhenMultipleSpacesProvided_ShouldCollapse()
    {
        var result = Library.ToSlug("Hello   World");

        Assert.AreEqual("hello-world", result);
    }

    [TestMethod]
    public void ToSlug_WhenUnderscoresProvided_ShouldConvertToDashes()
    {
        var result = Library.ToSlug("hello_world");

        Assert.AreEqual("hello-world", result);
    }

    #endregion

    #region EscapeRegex Tests

    [TestMethod]
    public void EscapeRegex_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.EscapeRegex(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void EscapeRegex_WhenSpecialCharsProvided_ShouldEscape()
    {
        var result = Library.EscapeRegex("test.+*?");

        Assert.IsNotNull(result);
        Assert.Contains(@"\.", result);
        Assert.Contains(@"\+", result);
        Assert.Contains(@"\*", result);
        Assert.Contains(@"\?", result);
    }

    [TestMethod]
    public void EscapeRegex_WhenNoSpecialChars_ShouldReturnSame()
    {
        var result = Library.EscapeRegex("hello");

        Assert.AreEqual("hello", result);
    }

    #endregion

    #region EscapeSql Tests

    [TestMethod]
    public void EscapeSql_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.EscapeSql(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void EscapeSql_WhenSingleQuotesProvided_ShouldDouble()
    {
        var result = Library.EscapeSql("It's a test");

        Assert.AreEqual("It''s a test", result);
    }

    [TestMethod]
    public void EscapeSql_WhenNoQuotes_ShouldReturnSame()
    {
        var result = Library.EscapeSql("hello world");

        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void EscapeSql_WhenMultipleQuotes_ShouldDoubleAll()
    {
        var result = Library.EscapeSql("'test'");

        Assert.AreEqual("''test''", result);
    }

    #endregion

    #region ExtractUrls Tests

    [TestMethod]
    public void ExtractUrls_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ExtractUrls(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractUrls_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ExtractUrls(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractUrls_WhenNoUrlsProvided_ShouldReturnEmpty()
    {
        var result = Library.ExtractUrls("no urls here");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ExtractUrls_WhenSingleUrlProvided_ShouldExtract()
    {
        var result = Library.ExtractUrls("Visit https://example.com today");

        Assert.AreEqual("https://example.com", result);
    }

    [TestMethod]
    public void ExtractUrls_WhenMultipleUrlsProvided_ShouldExtractAll()
    {
        var result = Library.ExtractUrls("Visit https://example.com and http://test.com");

        Assert.IsTrue(result?.Contains("https://example.com"));
        Assert.IsTrue(result?.Contains("http://test.com"));
    }

    #endregion

    #region ExtractEmails Tests

    [TestMethod]
    public void ExtractEmails_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ExtractEmails(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractEmails_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ExtractEmails(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractEmails_WhenNoEmailsProvided_ShouldReturnEmpty()
    {
        var result = Library.ExtractEmails("no emails here");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ExtractEmails_WhenSingleEmailProvided_ShouldExtract()
    {
        var result = Library.ExtractEmails("Contact test@example.com for info");

        Assert.AreEqual("test@example.com", result);
    }

    [TestMethod]
    public void ExtractEmails_WhenMultipleEmailsProvided_ShouldExtractAll()
    {
        var result = Library.ExtractEmails("Contact test@example.com or admin@example.com");

        Assert.IsTrue(result?.Contains("test@example.com"));
        Assert.IsTrue(result?.Contains("admin@example.com"));
    }

    #endregion

    #region ExtractIPs Tests

    [TestMethod]
    public void ExtractIPs_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ExtractIPs(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractIPs_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ExtractIPs(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractIPs_WhenNoIPsProvided_ShouldReturnEmpty()
    {
        var result = Library.ExtractIPs("no ips here");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ExtractIPs_WhenSingleIPProvided_ShouldExtract()
    {
        var result = Library.ExtractIPs("Server at 192.168.1.1 is running");

        Assert.AreEqual("192.168.1.1", result);
    }

    [TestMethod]
    public void ExtractIPs_WhenMultipleIPsProvided_ShouldExtractAll()
    {
        var result = Library.ExtractIPs("Servers 192.168.1.1 and 10.0.0.1 are running");

        Assert.IsTrue(result?.Contains("192.168.1.1"));
        Assert.IsTrue(result?.Contains("10.0.0.1"));
    }

    #endregion
}
