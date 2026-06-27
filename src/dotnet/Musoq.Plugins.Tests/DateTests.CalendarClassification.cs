using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTests
{
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
        Assert.IsTrue(result is >= 1 and <= 53);
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
}
