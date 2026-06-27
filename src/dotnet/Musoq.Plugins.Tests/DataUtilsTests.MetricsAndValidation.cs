using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DataUtilsTests
{
    #region ToHumanReadableSize Tests

    [TestMethod]
    public void ToHumanReadableSize_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ToHumanReadableSize(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenZeroProvided_ShouldReturnZeroB()
    {
        var result = Library.ToHumanReadableSize(0);

        Assert.AreEqual("0 B", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenBytesProvided_ShouldReturnBytes()
    {
        var result = Library.ToHumanReadableSize(500);

        Assert.AreEqual("500 B", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenKilobytesProvided_ShouldReturnKB()
    {
        var result = Library.ToHumanReadableSize(1536);

        Assert.IsNotNull(result);
        Assert.Contains("KB", result);
        Assert.Contains("1", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenMegabytesProvided_ShouldReturnMB()
    {
        var result = Library.ToHumanReadableSize(1048576);

        Assert.AreEqual("1 MB", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenGigabytesProvided_ShouldReturnGB()
    {
        var result = Library.ToHumanReadableSize(1073741824L);

        Assert.AreEqual("1 GB", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenTerabytesProvided_ShouldReturnTB()
    {
        var result = Library.ToHumanReadableSize(1099511627776L);

        Assert.AreEqual("1 TB", result);
    }

    #endregion

    #region ToHumanReadableDuration Tests

    [TestMethod]
    public void ToHumanReadableDuration_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ToHumanReadableDuration(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenZeroProvided_ShouldReturn0s()
    {
        var result = Library.ToHumanReadableDuration(0);

        Assert.AreEqual("0s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenSecondsOnlyProvided_ShouldReturnSeconds()
    {
        var result = Library.ToHumanReadableDuration(45);

        Assert.AreEqual("45s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenMinutesProvided_ShouldReturnMinutesAndSeconds()
    {
        var result = Library.ToHumanReadableDuration(90);

        Assert.AreEqual("1m 30s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenHoursProvided_ShouldReturnHoursMinutesSeconds()
    {
        var result = Library.ToHumanReadableDuration(3661);

        Assert.AreEqual("1h 1m 1s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenDaysProvided_ShouldReturnDaysHoursMinutesSeconds()
    {
        var result = Library.ToHumanReadableDuration(90061);

        Assert.AreEqual("1d 1h 1m 1s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenExactHourProvided_ShouldNotShowZeroComponents()
    {
        var result = Library.ToHumanReadableDuration(3600);

        Assert.AreEqual("1h", result);
    }

    #endregion

    #region CalculateEntropy Tests

    [TestMethod]
    public void CalculateEntropy_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.CalculateEntropy(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void CalculateEntropy_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.CalculateEntropy(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void CalculateEntropy_WhenSingleCharRepeated_ShouldReturnZero()
    {
        var result = Library.CalculateEntropy("aaaa");

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.001, Math.Abs(result.Value - 0.0));
    }

    [TestMethod]
    public void CalculateEntropy_WhenAllUniqueChars_ShouldReturnHighEntropy()
    {
        var result = Library.CalculateEntropy("abcd");

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(1.5, result.Value);
    }

    [TestMethod]
    public void CalculateEntropy_WhenRandomStringProvided_ShouldReturnPositiveValue()
    {
        var result = Library.CalculateEntropy("Hello World!");

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Value);
    }

    #endregion

}
