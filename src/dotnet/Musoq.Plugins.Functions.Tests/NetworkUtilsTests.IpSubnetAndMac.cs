using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsTests
{
    #region IsPrivateIP Tests

    [TestMethod]
    public void IsPrivateIP_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsPrivateIp(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenEmptyStringProvided_ShouldReturnNull()
    {
        var result = Library.IsPrivateIp(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenInvalidIPProvided_ShouldReturnNull()
    {
        var result = Library.IsPrivateIp("not-an-ip");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsPrivateIP_When10NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("10.0.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When10NetworkMaxProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("10.255.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_16NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("172.16.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_31NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("172.31.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_15NetworkProvided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIp("172.15.0.1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrivateIP_When172_32NetworkProvided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIp("172.32.0.1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrivateIP_When192_168NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("192.168.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When192_168MaxProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("192.168.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenLocalhostProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("127.0.0.1");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_When127NetworkProvided_ShouldReturnTrue()
    {
        var result = Library.IsPrivateIp("127.255.255.255");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenPublicIPProvided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIp("8.8.8.8");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrivateIP_WhenIPv6Provided_ShouldReturnFalse()
    {
        var result = Library.IsPrivateIp("::1");

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


        Assert.AreEqual(3232235777L, result);
    }

    [TestMethod]
    public void IpToLong_When10_0_0_1Provided_ShouldReturnCorrectValue()
    {
        var result = Library.IpToLong("10.0.0.1");


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
}
