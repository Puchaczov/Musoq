using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for network utility methods to improve branch coverage.
///     Tests IsPrivateIP, IpToLong, LongToIp, IsInSubnet, FormatMac, ConvertBase,
///     Unix timestamps, ToSlug, EscapeRegex, EscapeSql, ExtractUrls, ExtractEmails, ExtractIPs.
/// </summary>
[TestClass]
public partial class NetworkUtilsExtendedTests : PluginsTestBase
{
    #region IsPrivateIP Tests

    [TestMethod]
    public void IsPrivateIP_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsPrivateIp(null));
    }

    [TestMethod]
    public void IsPrivateIP_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.IsPrivateIp(string.Empty));
    }

    [TestMethod]
    public void IsPrivateIP_InvalidIP_ReturnsNull()
    {
        Assert.IsNull(Library.IsPrivateIp("not an ip"));
    }

    [TestMethod]
    public void IsPrivateIP_10Network_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIp("10.0.0.1"));
        Assert.IsTrue(Library.IsPrivateIp("10.255.255.255"));
    }

    [TestMethod]
    public void IsPrivateIP_172Network_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIp("172.16.0.1"));
        Assert.IsTrue(Library.IsPrivateIp("172.31.255.255"));
    }

    [TestMethod]
    public void IsPrivateIP_172NetworkOutsideRange_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsPrivateIp("172.15.0.1"));
        Assert.IsFalse(Library.IsPrivateIp("172.32.0.1"));
    }

    [TestMethod]
    public void IsPrivateIP_192168Network_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIp("192.168.0.1"));
        Assert.IsTrue(Library.IsPrivateIp("192.168.255.255"));
    }

    [TestMethod]
    public void IsPrivateIP_Localhost_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsPrivateIp("127.0.0.1"));
    }

    [TestMethod]
    public void IsPrivateIP_PublicIP_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsPrivateIp("8.8.8.8"));
        Assert.IsFalse(Library.IsPrivateIp("1.1.1.1"));
    }

    [TestMethod]
    public void IsPrivateIP_IPv6_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsPrivateIp("::1"));
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

}
