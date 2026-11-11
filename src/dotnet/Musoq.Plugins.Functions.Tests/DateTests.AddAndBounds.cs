using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTests
{
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
}
