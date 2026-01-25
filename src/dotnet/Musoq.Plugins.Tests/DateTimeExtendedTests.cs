using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for DateTime methods in LibraryBaseDate.cs to improve branch coverage
/// </summary>
[TestClass]
public class DateTimeExtendedTests : LibraryBaseBaseTests
{
    #region Month/Year/Day/Hour/Minute/Second Tests

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
    public void Hour_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Hour(null));
    }

    [TestMethod]
    public void Hour_ValidDateTimeOffset_ReturnsHour()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(12, Library.Hour(date));
    }

    [TestMethod]
    public void Minute_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Minute(null));
    }

    [TestMethod]
    public void Minute_ValidDateTimeOffset_ReturnsMinute()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(30, Library.Minute(date));
    }

    [TestMethod]
    public void Second_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Second(null));
    }

    [TestMethod]
    public void Second_ValidDateTimeOffset_ReturnsSecond()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual(45, Library.Second(date));
    }

    [TestMethod]
    public void Milliseconds_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.Milliseconds(null));
    }

    [TestMethod]
    public void Milliseconds_ValidDateTimeOffset_ReturnsMilliseconds()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, 123, TimeSpan.Zero);
        Assert.AreEqual(123, Library.Milliseconds(date));
    }

    [TestMethod]
    public void DayOfWeek_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.DayOfWeek(null));
    }

    [TestMethod]
    public void DayOfWeek_ValidDateTimeOffset_ReturnsDayOfWeek()
    {
        var date = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
        Assert.AreEqual((int)DayOfWeek.Saturday, Library.DayOfWeek(date));
    }

    #endregion

    #region AddDays Tests

    [TestMethod]
    public void AddDays_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.AddDays(null, 5));
    }

    [TestMethod]
    public void AddDays_ValidDateTime_AddsDays()
    {
        var date = new DateTime(2024, 6, 15);
        var result = Library.AddDays(date, 5);
        Assert.AreEqual(new DateTime(2024, 6, 20), result);
    }

    [TestMethod]
    public void AddDays_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.AddDays((DateTimeOffset?)null, 5));
    }

    [TestMethod]
    public void AddDays_ValidDateTimeOffset_AddsDays()
    {
        var date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var result = Library.AddDays(date, 5);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 20, 0, 0, 0, TimeSpan.Zero), result);
    }

    #endregion

    #region AddMonths Tests

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

    #endregion

    #region AddYears Tests

    [TestMethod]
    public void AddYears_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.AddYears(null, 1));
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

    #endregion

    #region AddHours Tests

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

    #endregion

    #region AddMinutes Tests

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
    public void AddMinutes_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.AddMinutes((DateTimeOffset?)null, 30));
    }

    [TestMethod]
    public void AddMinutes_ValidDateTimeOffset_AddsMinutes()
    {
        var date = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var result = Library.AddMinutes(date, 30);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero), result);
    }

    #endregion

    #region AddSeconds Tests

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
    public void AddSeconds_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.AddSeconds((DateTimeOffset?)null, 45));
    }

    [TestMethod]
    public void AddSeconds_ValidDateTimeOffset_AddsSeconds()
    {
        var date = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var result = Library.AddSeconds(date, 45);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 15, 10, 0, 45, TimeSpan.Zero), result);
    }

    #endregion

    #region StartOfDay Tests

    [TestMethod]
    public void StartOfDay_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.StartOfDay(null));
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

    #endregion

    #region EndOfDay Tests

    [TestMethod]
    public void EndOfDay_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.EndOfDay(null));
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

    #endregion

    #region IsWeekend Tests

    [TestMethod]
    public void IsWeekend_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekend(null));
    }

    [TestMethod]
    public void IsWeekend_Saturday_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.AreEqual(true, Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_Sunday_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 16);
        Assert.AreEqual(true, Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_Monday_ReturnsFalse()
    {
        var date = new DateTime(2024, 6, 17);
        Assert.AreEqual(false, Library.IsWeekend(date));
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
        Assert.AreEqual(true, Library.IsWeekend(date));
    }

    #endregion

    #region IsWeekday Tests

    [TestMethod]
    public void IsWeekday_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekday(null));
    }

    [TestMethod]
    public void IsWeekday_Monday_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 17);
        Assert.AreEqual(true, Library.IsWeekday(date));
    }

    [TestMethod]
    public void IsWeekday_Saturday_ReturnsFalse()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.AreEqual(false, Library.IsWeekday(date));
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
        Assert.AreEqual(true, Library.IsWeekday(date));
    }

    #endregion

    #region DateDiffInDays Tests

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

    #endregion

    #region DateDiffInHours Tests

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

    #endregion

    #region DateDiffInMinutes Tests

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

    #endregion

    #region DateDiffInSeconds Tests

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

    #endregion

    #region WeekOfYear Tests

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
        Assert.IsTrue(result >= 1 && result <= 53);
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
        Assert.IsTrue(result >= 1 && result <= 53);
    }

    #endregion

    #region Quarter Tests

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

    #endregion

    #region DayOfYear Tests

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

    #endregion

    #region IsLeapYear Tests

    [TestMethod]
    public void IsLeapYear_NullDateTime_ReturnsNull()
    {
        Assert.IsNull(Library.IsLeapYear(null));
    }

    [TestMethod]
    public void IsLeapYear_2024_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsLeapYear(new DateTime(2024, 1, 1)));
    }

    [TestMethod]
    public void IsLeapYear_2023_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsLeapYear(new DateTime(2023, 1, 1)));
    }

    [TestMethod]
    public void IsLeapYear_NullDateTimeOffset_ReturnsNull()
    {
        Assert.IsNull(Library.IsLeapYear((DateTimeOffset?)null));
    }

    [TestMethod]
    public void IsLeapYear_DateTimeOffset_2024_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsLeapYear(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    #endregion

    #region IsBetween Tests

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
        Assert.AreEqual(true,
            Library.IsBetween(new DateTime(2024, 6, 15), new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetween_ValueOutOfRange_ReturnsFalse()
    {
        Assert.AreEqual(false,
            Library.IsBetween(new DateTime(2025, 6, 15), new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
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
        Assert.AreEqual(true, Library.IsBetween(
            new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)));
    }

    #endregion

    #region IsBetweenExclusive Tests

    [TestMethod]
    public void IsBetweenExclusive_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.IsBetweenExclusive(null, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetweenExclusive_ValueAtStart_ReturnsFalse()
    {
        Assert.AreEqual(false,
            Library.IsBetweenExclusive(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetweenExclusive_ValueAtEnd_ReturnsFalse()
    {
        Assert.AreEqual(false,
            Library.IsBetweenExclusive(new DateTime(2024, 12, 31), new DateTime(2024, 1, 1),
                new DateTime(2024, 12, 31)));
    }

    [TestMethod]
    public void IsBetweenExclusive_ValueInRange_ReturnsTrue()
    {
        Assert.AreEqual(true,
            Library.IsBetweenExclusive(new DateTime(2024, 6, 15), new DateTime(2024, 1, 1),
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
        Assert.AreEqual(true, Library.IsBetweenExclusive(
            new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)));
    }

    #endregion

    #region ExtractTimeSpan Tests

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

    #endregion

    #region ExtractFromDate Tests

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

    #endregion
}
