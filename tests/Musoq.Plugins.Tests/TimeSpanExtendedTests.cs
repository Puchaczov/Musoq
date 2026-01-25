using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for TimeSpan methods to improve branch coverage.
/// </summary>
[TestClass]
public class TimeSpanExtendedTests : LibraryBaseBaseTests
{
    #region AddTimeSpans Tests

    [TestMethod]
    public void AddTimeSpans_AllNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddTimeSpans(null, null, null));
    }

    [TestMethod]
    public void AddTimeSpans_SingleValue_ReturnsValue()
    {
        var ts = TimeSpan.FromHours(1);
        Assert.AreEqual(ts, Library.AddTimeSpans(ts));
    }

    [TestMethod]
    public void AddTimeSpans_TwoValues_ReturnsSum()
    {
        var ts1 = TimeSpan.FromHours(1);
        var ts2 = TimeSpan.FromMinutes(30);
        var result = Library.AddTimeSpans(ts1, ts2);
        Assert.AreEqual(TimeSpan.FromMinutes(90), result);
    }

    [TestMethod]
    public void AddTimeSpans_WithNulls_IgnoresNulls()
    {
        var ts1 = TimeSpan.FromHours(1);
        var result = Library.AddTimeSpans(null, ts1, null);
        Assert.AreEqual(ts1, result);
    }

    [TestMethod]
    public void AddTimeSpans_MultipleValues_ReturnsSum()
    {
        var result = Library.AddTimeSpans(
            TimeSpan.FromHours(1),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromSeconds(45));
        Assert.AreEqual(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(45), result);
    }

    [TestMethod]
    public void AddTimeSpans_FirstNullSecondValue_ReturnsSecondValue()
    {
        var ts = TimeSpan.FromHours(2);
        var result = Library.AddTimeSpans(null, ts);
        Assert.AreEqual(ts, result);
    }

    #endregion

    #region SubtractTimeSpans Tests

    [TestMethod]
    public void SubtractTimeSpans_AllNull_ReturnsNull()
    {
        Assert.IsNull(Library.SubtractTimeSpans(null, null, null));
    }

    [TestMethod]
    public void SubtractTimeSpans_SingleValue_ReturnsValue()
    {
        var ts = TimeSpan.FromHours(2);
        Assert.AreEqual(ts, Library.SubtractTimeSpans(ts));
    }

    [TestMethod]
    public void SubtractTimeSpans_TwoValues_ReturnsDifference()
    {
        var ts1 = TimeSpan.FromHours(2);
        var ts2 = TimeSpan.FromMinutes(30);
        var result = Library.SubtractTimeSpans(ts1, ts2);
        Assert.AreEqual(TimeSpan.FromMinutes(90), result);
    }

    [TestMethod]
    public void SubtractTimeSpans_WithNulls_IgnoresNulls()
    {
        var ts1 = TimeSpan.FromHours(2);
        var result = Library.SubtractTimeSpans(null, ts1, null);
        Assert.AreEqual(ts1, result);
    }

    [TestMethod]
    public void SubtractTimeSpans_FirstNullSecondValue_ReturnsSecondValue()
    {
        var ts = TimeSpan.FromHours(2);
        var result = Library.SubtractTimeSpans(null, ts);
        Assert.AreEqual(ts, result);
    }

    #endregion

    #region FromString Tests

    [TestMethod]
    public void FromString_ValidTimeSpan_ReturnsTimeSpan()
    {
        var result = Library.FromString("01:30:00");
        Assert.AreEqual(TimeSpan.FromMinutes(90), result);
    }

    [TestMethod]
    public void FromString_InvalidTimeSpan_ReturnsNull()
    {
        Assert.IsNull(Library.FromString("not a timespan"));
    }

    [TestMethod]
    public void FromString_DaysFormat_ReturnsTimeSpan()
    {
        var result = Library.FromString("1.02:30:00");
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Value.Days);
        Assert.AreEqual(2, result.Value.Hours);
        Assert.AreEqual(30, result.Value.Minutes);
    }

    #endregion
}
