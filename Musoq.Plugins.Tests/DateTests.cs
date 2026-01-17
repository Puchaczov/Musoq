using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class DateTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void ExtractFromDateTest()
    {
        Assert.AreEqual(2, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "month"));
        Assert.AreEqual(1, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "day"));
        Assert.AreEqual(2001, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "year"));
        Assert.AreEqual(9, Library.ExtractFromDate("01/02/2001 09:00:00 +00:00", "hour"));
        Assert.AreEqual(9, Library.ExtractFromDate("01/02/2001 00:09:00 +00:00", "minute"));
        Assert.AreEqual(9, Library.ExtractFromDate("01/02/2001 00:00:09 +00:00", "second"));
    }

    [TestMethod]
    public void ExtractFromDateWrongDateTest()
    {
        Assert.Throws<NotSupportedException>(() => Library.ExtractFromDate("error", "month"));
    }

    [TestMethod]
    public void ExtractFromDateWrongPartOfDateTest()
    {
        Assert.Throws<NotSupportedException>(() => Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "error"));
    }

    [TestMethod]
    public void ExtractFromDateWithCultureInfoTest()
    {
        Assert.AreEqual(2, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "pl-PL", "month"));
        Assert.AreEqual(1, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "pl-PL", "day"));
        Assert.AreEqual(2001, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "pl-PL", "year"));
    }

    [TestMethod]
    public void YearTest()
    {
        Assert.AreEqual(2012, Library.Year(DateTimeOffset.Parse("01/01/2012 00:00:00 +00:00")));
        Assert.IsNull(Library.Year(null));
    }

    [TestMethod]
    public void MonthTest()
    {
        Assert.AreEqual(2, Library.Month(DateTimeOffset.Parse("01/02/2012 00:00:00 +00:00")));
        Assert.IsNull(Library.Month(null));
    }

    [TestMethod]
    public void DayTest()
    {
        Assert.AreEqual(1, Library.Day(DateTimeOffset.Parse("01/02/2012 00:00:00 +00:00")));
        Assert.IsNull(Library.Day(null));
    }

    [TestMethod]
    public void ToTimeSpanTest()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(10), Library.ToTimeSpan("00:00:10"));
        Assert.AreEqual(TimeSpan.FromSeconds(70), Library.ToTimeSpan("00:01:10"));
    }

    [TestMethod]
    public void ToDateTimeTest()
    {
        Assert.AreEqual(new DateTime(2012, 10, 15), Library.ToDateTime("2012/10/15"));
    }

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

    #region AddMonths Tests

    [TestMethod]
    public void AddMonths_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddMonths(null, 1));
    }

    [TestMethod]
    public void AddMonths_DateTime_AddsMonths()
    {
        var date = new DateTime(2023, 1, 15);
        var result = Library.AddMonths(date, 3);
        Assert.AreEqual(new DateTime(2023, 4, 15), result);
    }

    [TestMethod]
    public void AddMonths_DateTime_SubtractsMonths()
    {
        var date = new DateTime(2023, 6, 15);
        var result = Library.AddMonths(date, -2);
        Assert.AreEqual(new DateTime(2023, 4, 15), result);
    }

    [TestMethod]
    public void AddMonths_DateTimeOffset_AddsMonths()
    {
        var date = new DateTimeOffset(2023, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var result = Library.AddMonths(date, 3);
        Assert.AreEqual(new DateTimeOffset(2023, 4, 15, 10, 30, 0, TimeSpan.Zero), result);
    }

    #endregion

    #region AddYears Tests

    [TestMethod]
    public void AddYears_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddYears(null, 1));
    }

    [TestMethod]
    public void AddYears_DateTime_AddsYears()
    {
        var date = new DateTime(2023, 6, 15);
        var result = Library.AddYears(date, 5);
        Assert.AreEqual(new DateTime(2028, 6, 15), result);
    }

    [TestMethod]
    public void AddYears_DateTimeOffset_AddsYears()
    {
        var date = new DateTimeOffset(2023, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var result = Library.AddYears(date, 2);
        Assert.AreEqual(new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero), result);
    }

    #endregion

    #region AddHours Tests

    [TestMethod]
    public void AddHours_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddHours(null, 1));
    }

    [TestMethod]
    public void AddHours_DateTime_AddsHours()
    {
        var date = new DateTime(2023, 6, 15, 10, 0, 0);
        var result = Library.AddHours(date, 5);
        Assert.AreEqual(new DateTime(2023, 6, 15, 15, 0, 0), result);
    }

    [TestMethod]
    public void AddHours_DateTime_CrossesMidnight()
    {
        var date = new DateTime(2023, 6, 15, 22, 0, 0);
        var result = Library.AddHours(date, 5);
        Assert.AreEqual(new DateTime(2023, 6, 16, 3, 0, 0), result);
    }

    #endregion

    #region AddMinutes Tests

    [TestMethod]
    public void AddMinutes_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddMinutes(null, 1));
    }

    [TestMethod]
    public void AddMinutes_DateTime_AddsMinutes()
    {
        var date = new DateTime(2023, 6, 15, 10, 30, 0);
        var result = Library.AddMinutes(date, 45);
        Assert.AreEqual(new DateTime(2023, 6, 15, 11, 15, 0), result);
    }

    #endregion

    #region AddSeconds Tests

    [TestMethod]
    public void AddSeconds_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddSeconds(null, 1));
    }

    [TestMethod]
    public void AddSeconds_DateTime_AddsSeconds()
    {
        var date = new DateTime(2023, 6, 15, 10, 30, 0);
        var result = Library.AddSeconds(date, 90);
        Assert.AreEqual(new DateTime(2023, 6, 15, 10, 31, 30), result);
    }

    #endregion

    #region StartOfDay Tests

    [TestMethod]
    public void StartOfDay_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.StartOfDay(null));
    }

    [TestMethod]
    public void StartOfDay_DateTime_ReturnsMidnight()
    {
        var date = new DateTime(2023, 6, 15, 14, 30, 45);
        var result = Library.StartOfDay(date);
        Assert.AreEqual(new DateTime(2023, 6, 15, 0, 0, 0), result);
    }

    [TestMethod]
    public void StartOfDay_DateTimeOffset_ReturnsMidnight()
    {
        var date = new DateTimeOffset(2023, 6, 15, 14, 30, 45, TimeSpan.FromHours(2));
        var result = Library.StartOfDay(date);
        Assert.AreEqual(new DateTimeOffset(2023, 6, 15, 0, 0, 0, TimeSpan.FromHours(2)), result);
    }

    #endregion

    #region EndOfDay Tests

    [TestMethod]
    public void EndOfDay_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.EndOfDay(null));
    }

    [TestMethod]
    public void EndOfDay_DateTime_ReturnsEndOfDay()
    {
        var date = new DateTime(2023, 6, 15, 14, 30, 45);
        var result = Library.EndOfDay(date);
        Assert.AreEqual(23, result?.Hour);
        Assert.AreEqual(59, result?.Minute);
        Assert.AreEqual(59, result?.Second);
    }

    [TestMethod]
    public void EndOfDay_DateTimeOffset_ReturnsEndOfDay()
    {
        var date = new DateTimeOffset(2023, 6, 15, 14, 30, 45, TimeSpan.FromHours(2));
        var result = Library.EndOfDay(date);
        Assert.AreEqual(23, result?.Hour);
        Assert.AreEqual(59, result?.Minute);
        Assert.AreEqual(59, result?.Second);
    }

    #endregion

    #region IsWeekend Tests

    [TestMethod]
    public void IsWeekend_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekend(null));
    }

    [TestMethod]
    public void IsWeekend_DateTime_Saturday_ReturnsTrue()
    {
        var date = new DateTime(2023, 6, 17);
        Assert.IsTrue(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_DateTime_Sunday_ReturnsTrue()
    {
        var date = new DateTime(2023, 6, 18);
        Assert.IsTrue(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_DateTime_Monday_ReturnsFalse()
    {
        var date = new DateTime(2023, 6, 19);
        Assert.IsFalse(Library.IsWeekend(date));
    }

    [TestMethod]
    public void IsWeekend_DateTime_Friday_ReturnsFalse()
    {
        var date = new DateTime(2023, 6, 16);
        Assert.IsFalse(Library.IsWeekend(date));
    }

    #endregion

    #region IsWeekday Tests

    [TestMethod]
    public void IsWeekday_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsWeekday(null));
    }

    [TestMethod]
    public void IsWeekday_DateTime_Monday_ReturnsTrue()
    {
        var date = new DateTime(2023, 6, 19);
        Assert.IsTrue(Library.IsWeekday(date));
    }

    [TestMethod]
    public void IsWeekday_DateTime_Saturday_ReturnsFalse()
    {
        var date = new DateTime(2023, 6, 17);
        Assert.IsFalse(Library.IsWeekday(date));
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

    #region WeekOfYear Tests

    [TestMethod]
    public void WeekOfYear_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.WeekOfYear(null));
    }

    [TestMethod]
    public void WeekOfYear_DateTime_ReturnsWeekNumber()
    {
        var date = new DateTime(2023, 6, 15);
        var result = Library.WeekOfYear(date);
        Assert.IsNotNull(result);
        Assert.IsTrue(result >= 1 && result <= 53);
    }

    #endregion

    #region Quarter Tests

    [TestMethod]
    public void Quarter_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Quarter(null));
    }

    [TestMethod]
    public void Quarter_DateTime_January_ReturnsQ1()
    {
        var date = new DateTime(2023, 1, 15);
        Assert.AreEqual(1, Library.Quarter(date));
    }

    [TestMethod]
    public void Quarter_DateTime_April_ReturnsQ2()
    {
        var date = new DateTime(2023, 4, 15);
        Assert.AreEqual(2, Library.Quarter(date));
    }

    [TestMethod]
    public void Quarter_DateTime_July_ReturnsQ3()
    {
        var date = new DateTime(2023, 7, 15);
        Assert.AreEqual(3, Library.Quarter(date));
    }

    [TestMethod]
    public void Quarter_DateTime_October_ReturnsQ4()
    {
        var date = new DateTime(2023, 10, 15);
        Assert.AreEqual(4, Library.Quarter(date));
    }

    #endregion

    #region DayOfYear Tests

    [TestMethod]
    public void DayOfYear_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.DayOfYear(null));
    }

    [TestMethod]
    public void DayOfYear_DateTime_Jan1_Returns1()
    {
        var date = new DateTime(2023, 1, 1);
        Assert.AreEqual(1, Library.DayOfYear(date));
    }

    [TestMethod]
    public void DayOfYear_DateTime_Dec31_Returns365()
    {
        var date = new DateTime(2023, 12, 31);
        Assert.AreEqual(365, Library.DayOfYear(date));
    }

    #endregion

    #region IsLeapYear Tests

    [TestMethod]
    public void IsLeapYear_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsLeapYear(null));
    }

    [TestMethod]
    public void IsLeapYear_DateTime_2024_ReturnsTrue()
    {
        var date = new DateTime(2024, 6, 15);
        Assert.IsTrue(Library.IsLeapYear(date));
    }

    [TestMethod]
    public void IsLeapYear_DateTime_2023_ReturnsFalse()
    {
        var date = new DateTime(2023, 6, 15);
        Assert.IsFalse(Library.IsLeapYear(date));
    }

    [TestMethod]
    public void IsLeapYear_DateTime_2000_ReturnsTrue()
    {
        var date = new DateTime(2000, 6, 15);
        Assert.IsTrue(Library.IsLeapYear(date));
    }

    [TestMethod]
    public void IsLeapYear_DateTime_1900_ReturnsFalse()
    {
        var date = new DateTime(1900, 6, 15);
        Assert.IsFalse(Library.IsLeapYear(date));
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

    #region AddDays Tests

    [TestMethod]
    public void AddDays_DateTime_WhenProvided_AddsDays()
    {
        var date = new DateTime(2024, 6, 15);
        var result = Library.AddDays(date, 5);
        Assert.AreEqual(new DateTime(2024, 6, 20), result);
    }

    [TestMethod]
    public void AddDays_DateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddDays(null, 5));
    }

    [TestMethod]
    public void AddDays_DateTimeOffset_WhenProvided_AddsDays()
    {
        var date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var result = Library.AddDays(date, 5);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 20, 0, 0, 0, TimeSpan.Zero), result);
    }

    [TestMethod]
    public void AddDays_DateTimeOffset_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.AddDays((DateTimeOffset?)null, 5));
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

    #region WeekOfYear Additional Tests

    [TestMethod]
    public void WeekOfYear_DateTime2024_ReturnsWeekNumber()
    {
        var date = new DateTime(2024, 1, 15);
        var result = Library.WeekOfYear(date);
        Assert.IsNotNull(result);
        Assert.IsTrue(result >= 1 && result <= 53);
    }

    [TestMethod]
    public void WeekOfYear_DateTimeOffset2024_ReturnsWeekNumber()
    {
        var date = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var result = Library.WeekOfYear(date);
        Assert.IsNotNull(result);
        Assert.IsTrue(result >= 1 && result <= 53);
    }

    #endregion
}