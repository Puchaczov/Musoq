using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for DateTime methods in LibraryBaseDate.cs to improve branch coverage
/// </summary>
[TestClass]
public partial class DateTimeExtendedTests : PluginsTestBase
{

    [TestMethod]
    public void Month_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Month(null));
    }

    [TestMethod]
    public void Month_ValidDateTimeOffset_ReturnsMonth()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(6, Library.Month(date));
    }

    [TestMethod]
    public void Year_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Year(null));
    }

    [TestMethod]
    public void Year_ValidDateTimeOffset_ReturnsYear()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(2024, Library.Year(date));
    }

    [TestMethod]
    public void Day_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Day(null));
    }

    [TestMethod]
    public void Day_ValidDateTimeOffset_ReturnsDay()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(15, Library.Day(date));
    }

    [TestMethod]
    public void Hour_ValidDateTimeOffset_ReturnsHour()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(12, Library.Hour(date));
    }

    [TestMethod]
    public void Minute_ValidDateTimeOffset_ReturnsMinute()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(30, Library.Minute(date));
    }

    [TestMethod]
    public void Second_ValidDateTimeOffset_ReturnsSecond()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(45, Library.Second(date));
    }

    [TestMethod]
    public void Milliseconds_ValidDateTimeOffset_ReturnsMilliseconds()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, 123, TimeSpan.Zero);
        Assert.AreEqual(123, Library.Milliseconds(date));
    }

    [TestMethod]
    public void DayOfWeek_ValidDateTimeOffset_ReturnsDayOfWeek()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual((int)DayOfWeek.Saturday, Library.DayOfWeek(date));
    }



    [TestMethod]
    public void AddMonths_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.AddMonths(null, 2));
    }

    [TestMethod]
    public void AddMonths_ValidDateTime_AddsMonths()
    {
        var date = new DateTime(2024, 6, 15);
        var result = Library.AddMonths(date, 2);
        Assert.AreEqual(new DateTime(2024, 8, 15), result);
    }

    [TestMethod]
    public void AddMonths_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.AddMonths((DateTimeOffset?)null, 2));
    }

    [TestMethod]
    public void AddMonths_ValidDateTimeOffset_AddsMonths()
    {
        var date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var result = Library.AddMonths(date, 2);
        Assert.AreEqual(new DateTimeOffset(2024, 8, 15, 0, 0, 0, TimeSpan.Zero), result);
    }



    [TestMethod]
    public void AddYears_ValidDateTime_AddsYears()
    {
        var date = new DateTime(2024, 6, 15);
        var result = Library.AddYears(date, 1);
        Assert.AreEqual(new DateTime(2025, 6, 15), result);
    }

    [TestMethod]
    public void AddYears_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.AddYears((DateTimeOffset?)null, 1));
    }

    [TestMethod]
    public void AddYears_ValidDateTimeOffset_AddsYears()
    {
        var date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var result = Library.AddYears(date, 1);
        Assert.AreEqual(new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero), result);
    }



    [TestMethod]
    public void AddHours_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.AddHours(null, 2));
    }

    [TestMethod]
    public void AddHours_ValidDateTime_AddsHours()
    {
        var date = new DateTime(2024, 6, 15, 10, 0, 0);
        var result = Library.AddHours(date, 2);
        Assert.AreEqual(new DateTime(2024, 6, 15, 12, 0, 0), result);
    }

    [TestMethod]
    public void AddHours_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.AddHours((DateTimeOffset?)null, 2));
    }

    [TestMethod]
    public void AddHours_ValidDateTimeOffset_AddsHours()
    {
        var date = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var result = Library.AddHours(date, 2);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero), result);
    }



    [TestMethod]
    public void AddMinutes_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.AddMinutes(null, 30));
    }

    [TestMethod]
    public void AddMinutes_ValidDateTime_AddsMinutes()
    {
        var date = new DateTime(2024, 6, 15, 10, 0, 0);
        var result = Library.AddMinutes(date, 30);
        Assert.AreEqual(new DateTime(2024, 6, 15, 10, 30, 0), result);
    }

    [TestMethod]
    public void AddSeconds_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.AddSeconds(null, 45));
    }

    [TestMethod]
    public void AddSeconds_ValidDateTime_AddsSeconds()
    {
        var date = new DateTime(2024, 6, 15, 10, 0, 0);
        var result = Library.AddSeconds(date, 45);
        Assert.AreEqual(new DateTime(2024, 6, 15, 10, 0, 45), result);
    }

    [TestMethod]
    public void StartOfDay_ValidDateTime_ReturnsMidnight()
    {
        var date = new DateTime(2024, 6, 15, 14, 30, 45);
        var result = Library.StartOfDay(date);
        Assert.AreEqual(new DateTime(2024, 6, 15, 0, 0, 0), result);
    }

    [TestMethod]
    public void StartOfDay_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.StartOfDay((DateTimeOffset?)null));
    }

    [TestMethod]
    public void StartOfDay_ValidDateTimeOffset_ReturnsMidnight()
    {
        var date = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.FromHours(2));
        var result = Library.StartOfDay(date);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.FromHours(2)), result);
    }



    [TestMethod]
    public void EndOfDay_ValidDateTime_ReturnsEndOfDay()
    {
        var date = new DateTime(2024, 6, 15, 14, 30, 45);
        var result = Library.EndOfDay(date);
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result.Value.Year);
        Assert.AreEqual(6, result.Value.Month);
        Assert.AreEqual(15, result.Value.Day);
        Assert.AreEqual(23, result.Value.Hour);
        Assert.AreEqual(59, result.Value.Minute);
        Assert.AreEqual(59, result.Value.Second);
    }

}
