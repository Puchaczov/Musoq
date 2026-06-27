using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTests
{
    #region GetDate Tests

    [TestMethod]
    public void GetDate_ReturnsCurrentDate()
    {
        var before = DateTimeOffset.Now;
        var result = Library.GetDate();
        var after = DateTimeOffset.Now;

        Assert.IsNotNull(result);
        Assert.IsTrue(result >= before);
        Assert.IsTrue(result <= after);
    }

    [TestMethod]
    public void UtcGetDate_ReturnsCurrentUtcDate()
    {
        var before = DateTimeOffset.UtcNow;
        var result = Library.UtcGetDate();
        var after = DateTimeOffset.UtcNow;

        Assert.IsNotNull(result);
        Assert.IsTrue(result >= before);
        Assert.IsTrue(result <= after);
    }

    #endregion

    #region Time Components Tests

    [TestMethod]
    public void Hour_WhenProvided_ReturnsHour()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(14, Library.Hour(dto));
    }

    [TestMethod]
    public void Hour_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Hour(null));
    }

    [TestMethod]
    public void Minute_WhenProvided_ReturnsMinute()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(30, Library.Minute(dto));
    }

    [TestMethod]
    public void Minute_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Minute(null));
    }

    [TestMethod]
    public void Second_WhenProvided_ReturnsSecond()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(45, Library.Second(dto));
    }

    [TestMethod]
    public void Second_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Second(null));
    }

    [TestMethod]
    public void Milliseconds_WhenProvided_ReturnsMilliseconds()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 45, 123, TimeSpan.Zero);
        Assert.AreEqual(123, Library.Milliseconds(dto));
    }

    [TestMethod]
    public void Milliseconds_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Milliseconds(null));
    }

    [TestMethod]
    public void DayOfWeek_WhenProvided_ReturnsDayOfWeek()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        Assert.AreEqual((int)DayOfWeek.Saturday, Library.DayOfWeek(dto));
    }

    [TestMethod]
    public void DayOfWeek_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.DayOfWeek(null));
    }

    #endregion

    #region ExtractTimeSpan Tests

    [TestMethod]
    public void ExtractTimeSpan_WhenProvided_ReturnsTimeOfDay()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.Zero);
        var result = Library.ExtractTimeSpan(dto);
        Assert.AreEqual(new TimeSpan(14, 30, 45), result);
    }

    [TestMethod]
    public void ExtractTimeSpan_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractTimeSpan(null));
    }

    #endregion
}
