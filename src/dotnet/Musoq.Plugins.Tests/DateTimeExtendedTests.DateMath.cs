using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTimeExtendedTests
{
    [TestMethod]
    public void EndOfDay_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.EndOfDay((DateTimeOffset?)null));
    }

    [TestMethod]
    public void EndOfDay_ValidDateTimeOffset_ReturnsEndOfDay()
    {
        var date = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.FromHours(2));
        var result = Library.EndOfDay(date);
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result.Value.Year);
        Assert.AreEqual(6, result.Value.Month);
        Assert.AreEqual(15, result.Value.Day);
        Assert.AreEqual(23, result.Value.Hour);
        Assert.AreEqual(59, result.Value.Minute);
    }



    [TestMethod]
    public void IsWeekend_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekend(null));
    }

    [TestMethod]
    public void IsWeekend_Saturday_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.IsTrue(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_Sunday_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 16);
        Assert.IsTrue(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_Monday_ReturnsFalse()
    {
        var date = new DateTime(2024, 6, 17);
        Assert.IsFalse(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekend((DateTimeOffset?)null));
    }

    [TestMethod]
    public void IsWeekend_DateTimeOffset_Saturday_ReturnsTrue()
    {
        var date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        Assert.IsTrue(Library.IsWeekend(date));
    }



    [TestMethod]
    public void IsWeekday_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekday(null));
    }

    [TestMethod]
    public void IsWeekday_Monday_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 17);
        Assert.IsTrue(Library.IsWeekday(date));
    }

    [TestMethod]
    public void IsWeekday_Saturday_ReturnsFalse()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.IsFalse(Library.IsWeekday(date));
    }

    [TestMethod]
    public void IsWeekday_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekday((DateTimeOffset?)null));
    }

    [TestMethod]
    public void IsWeekday_DateTimeOffset_Monday_ReturnsTrue()
    {
        var date = new DateTimeOffset(2024, 6, 17, 0, 0, 0, TimeSpan.Zero);
        Assert.IsTrue(Library.IsWeekday(date));
    }



    [TestMethod]
    public void DateDiffInDays_NullStartDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInDays(null, new DateTime(2024, 6, 20)));
    }

    [TestMethod]
    public void DateDiffInDays_NullEndDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInDays(new DateTime(2024, 6, 15), null));
    }

    [TestMethod]
    public void DateDiffInDays_ValidDates_ReturnsDifference()
    {
        var result = Library.DateDiffInDays(new DateTime(2024, 6, 15), new DateTime(2024, 6, 20));
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void DateDiffInDays_NullStartDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInDays(null, new DateTimeOffset(2024, 6, 20, 0, 0, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public void DateDiffInDays_ValidDateTimeOffsets_ReturnsDifference()
    {
        var result = Library.DateDiffInDays(
            new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 6, 20, 0, 0, 0, TimeSpan.Zero));
        Assert.AreEqual(5, result);
    }



    [TestMethod]
    public void DateDiffInHours_NullStartDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInHours(null, new DateTime(2024, 6, 15, 12, 0, 0)));
    }

    [TestMethod]
    public void DateDiffInHours_ValidDates_ReturnsDifference()
    {
        var result = Library.DateDiffInHours(new DateTime(2024, 6, 15, 10, 0, 0), new DateTime(2024, 6, 15, 12, 0, 0));
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void DateDiffInHours_NullStartDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInHours(null, new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public void DateDiffInHours_ValidDateTimeOffsets_ReturnsDifference()
    {
        var result = Library.DateDiffInHours(
            new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));
        Assert.AreEqual(2, result);
    }



    [TestMethod]
    public void DateDiffInMinutes_NullStartDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInMinutes(null, new DateTime(2024, 6, 15, 10, 30, 0)));
    }

    [TestMethod]
    public void DateDiffInMinutes_ValidDates_ReturnsDifference()
    {
        var result =
            Library.DateDiffInMinutes(new DateTime(2024, 6, 15, 10, 0, 0), new DateTime(2024, 6, 15, 10, 30, 0));
        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void DateDiffInMinutes_NullStartDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInMinutes(null, new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public void DateDiffInMinutes_ValidDateTimeOffsets_ReturnsDifference()
    {
        var result = Library.DateDiffInMinutes(
            new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero));
        Assert.AreEqual(30, result);
    }



    [TestMethod]
    public void DateDiffInSeconds_NullStartDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInSeconds(null, new DateTime(2024, 6, 15, 10, 0, 45)));
    }

    [TestMethod]
    public void DateDiffInSeconds_ValidDates_ReturnsDifference()
    {
        var result =
            Library.DateDiffInSeconds(new DateTime(2024, 6, 15, 10, 0, 0), new DateTime(2024, 6, 15, 10, 0, 45));
        Assert.AreEqual(45, result);
    }

    [TestMethod]
    public void DateDiffInSeconds_NullStartDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.DateDiffInSeconds(null, new DateTimeOffset(2024, 6, 15, 10, 0, 45, TimeSpan.Zero)));
    }

    [TestMethod]
    public void DateDiffInSeconds_ValidDateTimeOffsets_ReturnsDifference()
    {
        var result = Library.DateDiffInSeconds(
            new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 6, 15, 10, 0, 45, TimeSpan.Zero));
        Assert.AreEqual(45, result);
    }



    [TestMethod]
    public void WeekOfYear_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.WeekOfYear(null));
    }

    [TestMethod]
    public void WeekOfYear_ValidDateTime_ReturnsWeek()
    {
        var result = Library.WeekOfYear(new DateTime(2024, 1, 15));
        Assert.IsNotNull(result);
        Assert.IsTrue(result is >= 1 and <= 53);
    }

    [TestMethod]
    public void WeekOfYear_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.WeekOfYear((DateTimeOffset?)null));
    }

    [TestMethod]
    public void WeekOfYear_ValidDateTimeOffset_ReturnsWeek()
    {
        var result = Library.WeekOfYear(new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero));
        Assert.IsNotNull(result);
        Assert.IsTrue(result is >= 1 and <= 53);
    }



    [TestMethod]
    public void Quarter_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Quarter(null));
    }

    [TestMethod]
    public void Quarter_January_ReturnsQ1()
    {
        Assert.AreEqual(1, Library.Quarter(new DateTime(2024, 1, 15)));
    }

    [TestMethod]
    public void Quarter_April_ReturnsQ2()
    {
        Assert.AreEqual(2, Library.Quarter(new DateTime(2024, 4, 15)));
    }

    [TestMethod]
    public void Quarter_July_ReturnsQ3()
    {
        Assert.AreEqual(3, Library.Quarter(new DateTime(2024, 7, 15)));
    }

    [TestMethod]
    public void Quarter_October_ReturnsQ4()
    {
        Assert.AreEqual(4, Library.Quarter(new DateTime(2024, 10, 15)));
    }

    [TestMethod]
    public void Quarter_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Quarter((DateTimeOffset?)null));
    }

    [TestMethod]
    public void Quarter_DateTimeOffset_January_ReturnsQ1()
    {
        Assert.AreEqual(1, Library.Quarter(new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero)));
    }



    [TestMethod]
    public void DayOfYear_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.DayOfYear(null));
    }

    [TestMethod]
    public void DayOfYear_ValidDateTime_ReturnsDayOfYear()
    {
        Assert.AreEqual(1, Library.DayOfYear(new DateTime(2024, 1, 1)));
        Assert.AreEqual(166, Library.DayOfYear(new DateTime(2024, 6, 14)));
    }

    [TestMethod]
    public void DayOfYear_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.DayOfYear((DateTimeOffset?)null));
    }

    [TestMethod]
    public void DayOfYear_ValidDateTimeOffset_ReturnsDayOfYear()
    {
        Assert.AreEqual(1, Library.DayOfYear(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }



    [TestMethod]
    public void IsLeapYear_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.IsLeapYear(null));
    }

    [TestMethod]
    public void IsLeapYear_2024_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsLeapYear(new DateTime(2024, 1, 1)));
    }

}
