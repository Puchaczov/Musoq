using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsExtendedTests
{
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
}
