using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTests
{
    #region DateDiffInHours Tests

    [TestMethod]
    public void DateDiffInHours_DateTime_CalculatesDiff()
    {
        var start = new DateTime(2023, 1, 1, 10, 0, 0);
        var end = new DateTime(2023, 1, 1, 15, 0, 0);
        Assert.AreEqual(5, Library.DateDiffInHours(start, end));
    }

    #endregion

    #region DateDiffInMinutes Tests

    [TestMethod]
    public void DateDiffInMinutes_DateTime_CalculatesDiff()
    {
        var start = new DateTime(2023, 1, 1, 10, 0, 0);
        var end = new DateTime(2023, 1, 1, 10, 45, 0);
        Assert.AreEqual(45, Library.DateDiffInMinutes(start, end));
    }

    #endregion

    #region DateDiffInSeconds Tests

    [TestMethod]
    public void DateDiffInSeconds_DateTime_CalculatesDiff()
    {
        var start = new DateTime(2023, 1, 1, 10, 0, 0);
        var end = new DateTime(2023, 1, 1, 10, 1, 30);
        Assert.AreEqual(90, Library.DateDiffInSeconds(start, end));
    }

    #endregion

    #region DateDiffInDays Tests

    [TestMethod]
    public void DateDiffInDays_DateTime_WhenStartNull_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInDays(null, DateTime.Now));
    }

    [TestMethod]
    public void DateDiffInDays_DateTime_WhenEndNull_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInDays(DateTime.Now, null));
    }

    [TestMethod]
    public void DateDiffInDays_DateTime_CalculatesPositiveDiff()
    {
        var start = new DateTime(2023, 1, 1);
        var end = new DateTime(2023, 1, 11);
        Assert.AreEqual(10, Library.DateDiffInDays(start, end));
    }

    [TestMethod]
    public void DateDiffInDays_DateTime_CalculatesNegativeDiff()
    {
        var start = new DateTime(2023, 1, 11);
        var end = new DateTime(2023, 1, 1);
        Assert.AreEqual(-10, Library.DateDiffInDays(start, end));
    }

    #endregion

    #region IsBetween Tests

    [TestMethod]
    public void IsBetween_DateTime_WhenNull_ReturnsNull()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        Assert.IsNull(Library.IsBetween(null, start, end));
        Assert.IsNull(Library.IsBetween(new DateTime(2024, 6, 15), null, end));
        Assert.IsNull(Library.IsBetween(new DateTime(2024, 6, 15), start, null));
    }

    [TestMethod]
    public void IsBetween_DateTime_WhenInRange_ReturnsTrue()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        var date = new DateTime(2024, 6, 15);
        Assert.IsTrue(Library.IsBetween(date, start, end));
    }

    [TestMethod]
    public void IsBetween_DateTime_WhenAtBoundary_ReturnsTrue()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        Assert.IsTrue(Library.IsBetween(start, start, end));
        Assert.IsTrue(Library.IsBetween(end, start, end));
    }

    [TestMethod]
    public void IsBetween_DateTime_WhenOutOfRange_ReturnsFalse()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        Assert.IsFalse(Library.IsBetween(new DateTime(2023, 12, 31), start, end));
        Assert.IsFalse(Library.IsBetween(new DateTime(2025, 1, 1), start, end));
    }

    [TestMethod]
    public void IsBetween_DateTimeOffset_WhenInRange_ReturnsTrue()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var date = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.IsTrue(Library.IsBetween(date, start, end));
    }

    [TestMethod]
    public void IsBetweenExclusive_DateTime_WhenNull_ReturnsNull()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        Assert.IsNull(Library.IsBetweenExclusive(null, start, end));
    }

    [TestMethod]
    public void IsBetweenExclusive_DateTime_WhenInRange_ReturnsTrue()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        var date = new DateTime(2024, 6, 15);
        Assert.IsTrue(Library.IsBetweenExclusive(date, start, end));
    }

    [TestMethod]
    public void IsBetweenExclusive_DateTime_WhenAtBoundary_ReturnsFalse()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        Assert.IsFalse(Library.IsBetweenExclusive(start, start, end));
        Assert.IsFalse(Library.IsBetweenExclusive(end, start, end));
    }

    [TestMethod]
    public void IsBetweenExclusive_DateTimeOffset_WhenInRange_ReturnsTrue()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var date = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.IsTrue(Library.IsBetweenExclusive(date, start, end));
    }

    [TestMethod]
    public void IsBetweenExclusive_DateTimeOffset_WhenAtBoundary_ReturnsFalse()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
        Assert.IsFalse(Library.IsBetweenExclusive(start, start, end));
        Assert.IsFalse(Library.IsBetweenExclusive(end, start, end));
    }

    #endregion

    #region DateDiff Tests

    [TestMethod]
    public void DateDiffInDays_DateTime_CalculatesDifference()
    {
        var date1 = new DateTime(2024, 6, 10);
        var date2 = new DateTime(2024, 6, 15);
        Assert.AreEqual(5, Library.DateDiffInDays(date1, date2));
    }

    [TestMethod]
    public void DateDiffInDays_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInDays(null, new DateTime(2024, 6, 15)));
        Assert.IsNull(Library.DateDiffInDays(new DateTime(2024, 6, 15), null));
    }

    [TestMethod]
    public void DateDiffInDays_DateTimeOffset_CalculatesDifference()
    {
        var date1 = new DateTimeOffset(2024, 6, 10, 0, 0, 0, TimeSpan.Zero);
        var date2 = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        Assert.AreEqual(5, Library.DateDiffInDays(date1, date2));
    }

    [TestMethod]
    public void DateDiffInHours_DateTime_CalculatesDifference()
    {
        var date1 = new DateTime(2024, 6, 15, 10, 0, 0);
        var date2 = new DateTime(2024, 6, 15, 15, 0, 0);
        Assert.AreEqual(5, Library.DateDiffInHours(date1, date2));
    }

    [TestMethod]
    public void DateDiffInHours_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInHours(null, new DateTime(2024, 6, 15)));
    }

    [TestMethod]
    public void DateDiffInMinutes_DateTime_CalculatesDifference()
    {
        var date1 = new DateTime(2024, 6, 15, 10, 0, 0);
        var date2 = new DateTime(2024, 6, 15, 10, 30, 0);
        Assert.AreEqual(30, Library.DateDiffInMinutes(date1, date2));
    }

    [TestMethod]
    public void DateDiffInMinutes_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInMinutes(null, new DateTime(2024, 6, 15)));
    }

    [TestMethod]
    public void DateDiffInSeconds_DateTime_CalculatesDifference()
    {
        var date1 = new DateTime(2024, 6, 15, 10, 0, 0);
        var date2 = new DateTime(2024, 6, 15, 10, 0, 45);
        Assert.AreEqual(45, Library.DateDiffInSeconds(date1, date2));
    }

    [TestMethod]
    public void DateDiffInSeconds_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInSeconds(null, new DateTime(2024, 6, 15)));
    }

    #endregion
}
