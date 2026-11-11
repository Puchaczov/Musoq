using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DateTimeExtendedTests
{
    [TestMethod]
    public void ExtractTimeSpan_ValidDateTime_ReturnsTimeSpan()
    {
        var date = new DateTime(2024, 6, 15, 14, 30, 45, 123);
        var result = Library.ExtractTimeSpan(date);
        Assert.IsNotNull(result);
        Assert.AreEqual(14, result.Value.Hours);
        Assert.AreEqual(30, result.Value.Minutes);
        Assert.AreEqual(45, result.Value.Seconds);
    }



    [TestMethod]
    public void ToDateTimeWithFormat_WithCulture_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeWithFormat(null!, "yyyy-MM-dd", "en-US"));
    }

    [TestMethod]
    public void ToDateTimeWithFormat_WithCulture_EmptyValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeWithFormat(string.Empty, "yyyy-MM-dd", "en-US"));
    }

    [TestMethod]
    public void ToDateTimeWithFormat_WithCulture_ValidValue_ReturnsDateTime()
    {
        var result = Library.ToDateTimeWithFormat("15/06/2024", "dd/MM/yyyy", "en-GB");
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result.Value.Year);
        Assert.AreEqual(6, result.Value.Month);
        Assert.AreEqual(15, result.Value.Day);
    }

    [TestMethod]
    public void ToDateTimeWithFormat_WithCulture_InvalidFormat_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeWithFormat("2024-06-15", "dd/MM/yyyy HH:mm", "en-US"));
    }



    [TestMethod]
    public void ToDateTimeOffsetWithFormat_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeOffsetWithFormat(null!, "yyyy-MM-dd"));
    }

    [TestMethod]
    public void ToDateTimeOffsetWithFormat_EmptyValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeOffsetWithFormat(string.Empty, "yyyy-MM-dd"));
    }

    [TestMethod]
    public void ToDateTimeOffsetWithFormat_ValidValue_ReturnsDateTimeOffset()
    {
        var result = Library.ToDateTimeOffsetWithFormat("2024-06-15 14:30:00 +02:00", "yyyy-MM-dd HH:mm:ss zzz");
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result.Value.Year);
        Assert.AreEqual(6, result.Value.Month);
        Assert.AreEqual(15, result.Value.Day);
        Assert.AreEqual(14, result.Value.Hour);
        Assert.AreEqual(30, result.Value.Minute);
    }

    [TestMethod]
    public void ToDateTimeOffsetWithFormat_InvalidFormat_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeOffsetWithFormat("2024-06-15", "dd/MM/yyyy HH:mm"));
    }



    [TestMethod]
    public void ToDateTimeOffsetWithFormat_WithCulture_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeOffsetWithFormat(null!, "yyyy-MM-dd", "en-US"));
    }

    [TestMethod]
    public void ToDateTimeOffsetWithFormat_WithCulture_EmptyValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeOffsetWithFormat(string.Empty, "yyyy-MM-dd", "en-US"));
    }

    [TestMethod]
    public void ToDateTimeOffsetWithFormat_WithCulture_ValidValue_ReturnsDateTimeOffset()
    {
        var result = Library.ToDateTimeOffsetWithFormat("15/06/2024 14:30:00 +02:00", "dd/MM/yyyy HH:mm:ss zzz", "en-GB");
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result.Value.Year);
        Assert.AreEqual(6, result.Value.Month);
        Assert.AreEqual(15, result.Value.Day);
    }

    [TestMethod]
    public void ToDateTimeOffsetWithFormat_WithCulture_InvalidFormat_ReturnsNull()
    {
        Assert.IsNull(Library.ToDateTimeOffsetWithFormat("2024-06-15", "dd/MM/yyyy HH:mm", "en-US"));
    }

}
