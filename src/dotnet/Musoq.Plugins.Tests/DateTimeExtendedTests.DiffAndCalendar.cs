using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTimeExtendedTests
{
    [TestMethod]
    public void IsLeapYear_2023_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsLeapYear(new DateTime(2023, 1, 1)));
    }

    [TestMethod]
    public void IsLeapYear_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.IsLeapYear((DateTimeOffset?)null));
    }

    [TestMethod]
    public void IsLeapYear_DateTimeOffset_2024_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsLeapYear(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }



    [TestMethod]
    public void IsBetween_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.IsBetween(null, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetween_NullStart_ReturnsNull()
    {
        Assert.IsNull(Library.IsBetween(new DateTime(2024, 6, 15), null, new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetween_NullEnd_ReturnsNull()
    {
        Assert.IsNull(Library.IsBetween(new DateTime(2024, 6, 15), new DateTime(2024, 1, 1), null));
    }

    [TestMethod]
    public void IsBetween_ValueInRange_ReturnsTrue()
    {
        Assert.IsTrue(
            Library.IsBetween(new DateTime(2024, 6, 15), new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetween_ValueOutOfRange_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsBetween(new DateTime(2025, 6, 15), new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetween_DateTimeOffset_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.IsBetween(null,
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public void IsBetween_DateTimeOffset_ValueInRange_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsBetween(
            new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)));
    }



    [TestMethod]
    public void IsBetweenExclusive_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.IsBetweenExclusive(null, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetweenExclusive_ValueAtStart_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsBetweenExclusive(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetweenExclusive_ValueAtEnd_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsBetweenExclusive(new DateTime(2024, 12, 31), new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetweenExclusive_ValueInRange_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsBetweenExclusive(new DateTime(2024, 6, 15), new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetweenExclusive_DateTimeOffset_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.IsBetweenExclusive(null,
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public void IsBetweenExclusive_DateTimeOffset_ValueInRange_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsBetweenExclusive(
            new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)));
    }



    [TestMethod]
    public void ExtractTimeSpan_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractTimeSpan(null));
    }

    [TestMethod]
    public void ExtractTimeSpan_ValidDateTimeOffset_ReturnsTimeSpan()
    {
        var date = new DateTimeOffset(2024, 6, 15, 14, 30, 45, 123, TimeSpan.Zero);
        var result = Library.ExtractTimeSpan(date);
        Assert.IsNotNull(result);
        Assert.AreEqual(14, result.Value.Hours);
        Assert.AreEqual(30, result.Value.Minutes);
        Assert.AreEqual(45, result.Value.Seconds);
    }



    [TestMethod]
    public void ExtractFromDate_Month_ReturnsMonth()
    {
        var result = Library.ExtractFromDate("2024-06-15", "month");
        Assert.AreEqual(6, result);
    }

    [TestMethod]
    public void ExtractFromDate_Year_ReturnsYear()
    {
        var result = Library.ExtractFromDate("2024-06-15", "year");
        Assert.AreEqual(2024, result);
    }

    [TestMethod]
    public void ExtractFromDate_Day_ReturnsDay()
    {
        var result = Library.ExtractFromDate("2024-06-15", "day");
        Assert.AreEqual(15, result);
    }

    [TestMethod]
    public void ExtractFromDate_Hour_ReturnsHour()
    {
        var result = Library.ExtractFromDate("2024-06-15 14:30:45", "hour");
        Assert.AreEqual(14, result);
    }

    [TestMethod]
    public void ExtractFromDate_Minute_ReturnsMinute()
    {
        var result = Library.ExtractFromDate("2024-06-15 14:30:45", "minute");
        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void ExtractFromDate_Second_ReturnsSecond()
    {
        var result = Library.ExtractFromDate("2024-06-15 14:30:45", "second");
        Assert.AreEqual(45, result);
    }

    [TestMethod]
    public void ExtractFromDate_InvalidPart_ThrowsException()
    {
        var exceptionThrown = false;
        try
        {
            Library.ExtractFromDate("2024-06-15", "invalid");
        }
        catch (NotSupportedException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown, "NotSupportedException should be thrown");
    }

    [TestMethod]
    public void ExtractFromDate_InvalidDate_ThrowsException()
    {
        var exceptionThrown = false;
        try
        {
            Library.ExtractFromDate("not-a-date", "month");
        }
        catch (NotSupportedException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown, "NotSupportedException should be thrown");
    }

    [TestMethod]
    public void ExtractFromDate_WithCulture_ReturnsMonth()
    {
        var result = Library.ExtractFromDate("15/06/2024", "en-GB", "month");
        Assert.AreEqual(6, result);
    }



    [TestMethod]
    public void Year_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Year(null));
    }

    [TestMethod]
    public void Year_ValidDateTime_ReturnsYear()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 45);
        Assert.AreEqual(2024, Library.Year(date));
    }

    [TestMethod]
    public void Month_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Month(null));
    }

    [TestMethod]
    public void Month_ValidDateTime_ReturnsMonth()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 45);
        Assert.AreEqual(6, Library.Month(date));
    }

    [TestMethod]
    public void Day_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Day(null));
    }

    [TestMethod]
    public void Day_ValidDateTime_ReturnsDay()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 45);
        Assert.AreEqual(15, Library.Day(date));
    }

    [TestMethod]
    public void Hour_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Hour(null));
    }

    [TestMethod]
    public void Hour_ValidDateTime_ReturnsHour()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 45);
        Assert.AreEqual(12, Library.Hour(date));
    }

    [TestMethod]
    public void Minute_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Minute(null));
    }

    [TestMethod]
    public void Minute_ValidDateTime_ReturnsMinute()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 45);
        Assert.AreEqual(30, Library.Minute(date));
    }

    [TestMethod]
    public void Second_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Second(null));
    }

    [TestMethod]
    public void Second_ValidDateTime_ReturnsSecond()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 45);
        Assert.AreEqual(45, Library.Second(date));
    }

    [TestMethod]
    public void Milliseconds_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.Milliseconds(null));
    }

    [TestMethod]
    public void Milliseconds_ValidDateTime_ReturnsMilliseconds()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 45, 123);
        Assert.AreEqual(123, Library.Milliseconds(date));
    }

    [TestMethod]
    public void DayOfWeek_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.DayOfWeek(null));
    }

    [TestMethod]
    public void DayOfWeek_ValidDateTime_ReturnsDayOfWeek()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.AreEqual((int)DayOfWeek.Saturday, Library.DayOfWeek(date));
    }



    [TestMethod]
    public void ExtractTimeSpan_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractTimeSpan(null));
    }

}
