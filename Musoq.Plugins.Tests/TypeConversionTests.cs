using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class TypeConversionTests : LibraryBaseBaseTests
{
    #region ToChar Tests

    [TestMethod]
    public void ToChar_FromString_ShouldReturnFirstCharacter()
    {
        // Arrange
        string input = "hello";

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.AreEqual('h', result);
    }

    [TestMethod]
    public void ToChar_FromEmptyString_ShouldReturnNull()
    {
        // Arrange
        string input = "";

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_FromNullString_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_FromInt_ShouldReturnCharacter()
    {
        // Arrange
        int? input = 65; // ASCII 'A'

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.AreEqual('A', result);
    }

    [TestMethod]
    public void ToChar_FromNullInt_ShouldReturnNull()
    {
        // Arrange
        int? input = null;

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_FromShort_ShouldReturnCharacter()
    {
        // Arrange
        short? input = 66; // ASCII 'B'

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.AreEqual('B', result);
    }

    [TestMethod]
    public void ToChar_FromNullShort_ShouldReturnNull()
    {
        // Arrange
        short? input = null;

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_FromByte_ShouldReturnCharacter()
    {
        // Arrange
        byte? input = 67; // ASCII 'C'

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.AreEqual('C', result);
    }

    [TestMethod]
    public void ToChar_FromNullByte_ShouldReturnNull()
    {
        // Arrange
        byte? input = null;

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_FromObject_ShouldReturnCharacter()
    {
        // Arrange
        object input = 68; // ASCII 'D'

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.AreEqual('D', result);
    }

    [TestMethod]
    public void ToChar_FromNullObject_ShouldReturnNull()
    {
        // Arrange
        object? input = null;

        // Act
        var result = Library.ToChar(input);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region ToDateTime Tests

    [TestMethod]
    public void ToDateTime_FromValidString_ShouldReturnDateTime()
    {
        // Arrange
        string input = "2023-12-25";

        // Act
        var result = Library.ToDateTime(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2023, 12, 25), result);
    }

    [TestMethod]
    public void ToDateTime_FromInvalidString_ShouldReturnNull()
    {
        // Arrange
        string input = "invalid date";

        // Act
        var result = Library.ToDateTime(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDateTime_FromEmptyString_ShouldReturnNull()
    {
        // Arrange
        string input = "";

        // Act
        var result = Library.ToDateTime(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDateTime_WithCulture_ShouldRespectCulture()
    {
        // Arrange
        string input = "25/12/2023"; // UK date format
        string culture = "en-GB";

        // Act
        var result = Library.ToDateTime(input, culture);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2023, 12, 25), result);
    }

    [TestMethod]
    public void ToDateTime_WithInvalidCulture_ShouldReturnNull()
    {
        // Arrange
        string input = "25/12/2023";
        string culture = "en-US"; // US format expects MM/dd/yyyy

        // Act
        var result = Library.ToDateTime(input, culture);

        // Assert
        Assert.IsNull(result); // Should fail to parse 25 as month
    }

    #endregion

    #region SubtractDates Tests

    [TestMethod]
    public void SubtractDates_WithValidDates_ShouldReturnTimeSpan()
    {
        // Arrange
        DateTime date1 = new DateTime(2023, 12, 25);
        DateTime date2 = new DateTime(2023, 12, 20);

        // Act
        var result = Library.SubtractDates(date1, date2);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TimeSpan.FromDays(5), result);
    }

    [TestMethod]
    public void SubtractDates_WithFirstDateNull_ShouldReturnNull()
    {
        // Arrange
        DateTime? date1 = null;
        DateTime date2 = new DateTime(2023, 12, 20);

        // Act
        var result = Library.SubtractDates(date1, date2);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SubtractDates_WithSecondDateNull_ShouldReturnNull()
    {
        // Arrange
        DateTime date1 = new DateTime(2023, 12, 25);
        DateTime? date2 = null;

        // Act
        var result = Library.SubtractDates(date1, date2);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SubtractDates_WithBothDatesNull_ShouldReturnNull()
    {
        // Arrange
        DateTime? date1 = null;
        DateTime? date2 = null;

        // Act
        var result = Library.SubtractDates(date1, date2);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SubtractDates_WithNegativeResult_ShouldReturnNegativeTimeSpan()
    {
        // Arrange
        DateTime date1 = new DateTime(2023, 12, 20);
        DateTime date2 = new DateTime(2023, 12, 25);

        // Act
        var result = Library.SubtractDates(date1, date2);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TimeSpan.FromDays(-5), result);
    }

    #endregion

    #region ToTimeSpan Tests

    [TestMethod]
    public void ToTimeSpan_FromValidString_ShouldReturnTimeSpan()
    {
        // Arrange
        string input = "01:30:45";

        // Act
        var result = Library.ToTimeSpan(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(new TimeSpan(1, 30, 45), result);
    }

    [TestMethod]
    public void ToTimeSpan_FromInvalidString_ShouldReturnNull()
    {
        // Arrange
        string input = "invalid timespan";

        // Act
        var result = Library.ToTimeSpan(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToTimeSpan_FromNullString_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.ToTimeSpan(input!); // We know this will be null, suppressing warning

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToTimeSpan_FromDaysString_ShouldReturnTimeSpan()
    {
        // Arrange
        string input = "2.12:30:45"; // 2 days, 12 hours, 30 minutes, 45 seconds

        // Act
        var result = Library.ToTimeSpan(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(new TimeSpan(2, 12, 30, 45), result);
    }

    #endregion
}