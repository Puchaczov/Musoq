using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsTests
{
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
}
