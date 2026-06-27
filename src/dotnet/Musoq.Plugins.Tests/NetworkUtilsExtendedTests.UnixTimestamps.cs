using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsExtendedTests
{
    #region Unix Timestamp Tests

    [TestMethod]
    public void UnixToDateTime_Null_ReturnsNull()
    {
        Assert.IsNull(Library.UnixToDateTime(null));
    }

    [TestMethod]
    public void UnixToDateTime_Zero_Returns1970()
    {
        var result = Library.UnixToDateTime(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1970, result.Value.Year);
        Assert.AreEqual(1, result.Value.Month);
        Assert.AreEqual(1, result.Value.Day);
    }

    [TestMethod]
    public void UnixToDateTime_ValidTimestamp_ReturnsCorrectDate()
    {
        var result = Library.UnixToDateTime(1609459200);
        Assert.IsNotNull(result);
        Assert.AreEqual(2021, result.Value.Year);
    }

    [TestMethod]
    public void UnixMillisToDateTime_Null_ReturnsNull()
    {
        Assert.IsNull(Library.UnixMillisToDateTime(null));
    }

    [TestMethod]
    public void UnixMillisToDateTime_Zero_Returns1970()
    {
        var result = Library.UnixMillisToDateTime(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1970, result.Value.Year);
    }

    [TestMethod]
    public void DateTimeToUnix_Null_ReturnsNull()
    {
        Assert.IsNull(Library.DateTimeToUnix(null));
    }

    [TestMethod]
    public void DateTimeToUnix_ValidDateTime_ReturnsTimestamp()
    {
        var result = Library.DateTimeToUnix(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public void DateTimeToUnixMillis_Null_ReturnsNull()
    {
        Assert.IsNull(Library.DateTimeToUnixMillis(null));
    }

    [TestMethod]
    public void DateTimeToUnixMillis_ValidDateTime_ReturnsTimestamp()
    {
        var result = Library.DateTimeToUnixMillis(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public void UnixToDateTimeOffset_Null_ReturnsNull()
    {
        Assert.IsNull(Library.UnixToDateTimeOffset(null));
    }

    [TestMethod]
    public void UnixToDateTimeOffset_Zero_Returns1970()
    {
        var result = Library.UnixToDateTimeOffset(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1970, result.Value.Year);
        Assert.AreEqual(1, result.Value.Month);
        Assert.AreEqual(1, result.Value.Day);
    }

    [TestMethod]
    public void UnixToDateTimeOffset_ValidTimestamp_ReturnsCorrectDate()
    {
        var result = Library.UnixToDateTimeOffset(1609459200);
        Assert.IsNotNull(result);
        Assert.AreEqual(2021, result.Value.Year);
    }

    [TestMethod]
    public void UnixMillisToDateTimeOffset_Null_ReturnsNull()
    {
        Assert.IsNull(Library.UnixMillisToDateTimeOffset(null));
    }

    [TestMethod]
    public void UnixMillisToDateTimeOffset_Zero_Returns1970()
    {
        var result = Library.UnixMillisToDateTimeOffset(0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1970, result.Value.Year);
    }

    [TestMethod]
    public void DateTimeOffsetToUnix_Null_ReturnsNull()
    {
        Assert.IsNull(Library.DateTimeOffsetToUnix(null));
    }

    [TestMethod]
    public void DateTimeOffsetToUnix_ValidDateTimeOffset_ReturnsTimestamp()
    {
        var result = Library.DateTimeOffsetToUnix(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public void DateTimeOffsetToUnixMillis_Null_ReturnsNull()
    {
        Assert.IsNull(Library.DateTimeOffsetToUnixMillis(null));
    }

    [TestMethod]
    public void DateTimeOffsetToUnixMillis_ValidDateTimeOffset_ReturnsTimestamp()
    {
        var result = Library.DateTimeOffsetToUnixMillis(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.AreEqual(0L, result);
    }

    #endregion
}
