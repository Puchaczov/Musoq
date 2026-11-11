using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTests
{
    #region AddHours/AddMinutes/AddSeconds DateTimeOffset Tests

    [TestMethod]
    public void AddHours_DateTimeOffset_WhenProvided_AddsHours()
    {
        var date = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var result = Library.AddHours(date, 5);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 15, 15, 0, 0, TimeSpan.Zero), result);
    }

    [TestMethod]
    public void AddHours_DateTimeOffset_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddHours((DateTimeOffset?)null, 5));
    }

    [TestMethod]
    public void AddMinutes_DateTimeOffset_WhenProvided_AddsMinutes()
    {
        var date = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var result = Library.AddMinutes(date, 30);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero), result);
    }

    [TestMethod]
    public void AddMinutes_DateTimeOffset_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddMinutes((DateTimeOffset?)null, 30));
    }

    [TestMethod]
    public void AddSeconds_DateTimeOffset_WhenProvided_AddsSeconds()
    {
        var date = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var result = Library.AddSeconds(date, 45);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 15, 10, 0, 45, TimeSpan.Zero), result);
    }

    [TestMethod]
    public void AddSeconds_DateTimeOffset_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddSeconds((DateTimeOffset?)null, 45));
    }

    #endregion

    #region IsWeekend/IsWeekday Additional Tests

    [TestMethod]
    public void IsWeekend_DateTime_WhenSaturday2024_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.IsTrue(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_DateTime_WhenSunday2024_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 16);
        Assert.IsTrue(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_DateTime_WhenMonday2024_ReturnsFalse()
    {
        var date = new DateTime(2024, 6, 17);
        Assert.IsFalse(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_DateTimeOffset_WhenSaturday2024_ReturnsTrue()
    {
        var date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        Assert.IsTrue(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekday_DateTime_WhenMonday2024_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 17);
        Assert.IsTrue(Library.IsWeekday(date));
    }

    [TestMethod]
    public void IsWeekday_DateTime_WhenSaturday2024_ReturnsFalse()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.IsFalse(Library.IsWeekday(date));
    }

    [TestMethod]
    public void IsWeekday_DateTimeOffset_WhenMonday2024_ReturnsTrue()
    {
        var date = new DateTimeOffset(2024, 6, 17, 0, 0, 0, TimeSpan.Zero);
        Assert.IsTrue(Library.IsWeekday(date));
    }

    #endregion

    #region WeekOfYear Additional Tests

    [TestMethod]
    public void WeekOfYear_DateTime2024_ReturnsWeekNumber()
    {
        var date = new DateTime(2024, 1, 15);
        var result = Library.WeekOfYear(date);
        Assert.IsNotNull(result);
        Assert.IsTrue(result is >= 1 and <= 53);
    }

    [TestMethod]
    public void WeekOfYear_DateTimeOffset2024_ReturnsWeekNumber()
    {
        var date = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var result = Library.WeekOfYear(date);
        Assert.IsNotNull(result);
        Assert.IsTrue(result is >= 1 and <= 53);
    }

    #endregion
}
