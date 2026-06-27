using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsTests
{
    #region UnixToDateTime Tests

    [TestMethod]
    public void UnixToDateTime_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.UnixToDateTime(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void UnixToDateTime_WhenZeroProvided_ShouldReturnEpoch()
    {
        var result = Library.UnixToDateTime(0);

        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void UnixToDateTime_WhenValidTimestampProvided_ShouldConvert()
    {
        var result = Library.UnixToDateTime(1704067200);

        Assert.AreEqual(2024, result?.Year);
        Assert.AreEqual(1, result?.Month);
        Assert.AreEqual(1, result?.Day);
    }

    #endregion

    #region UnixMillisToDateTime Tests

    [TestMethod]
    public void UnixMillisToDateTime_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.UnixMillisToDateTime(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenZeroProvided_ShouldReturnEpoch()
    {
        var result = Library.UnixMillisToDateTime(0);

        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenValidTimestampProvided_ShouldConvert()
    {
        var result = Library.UnixMillisToDateTime(1704067200000);

        Assert.AreEqual(2024, result?.Year);
        Assert.AreEqual(1, result?.Month);
        Assert.AreEqual(1, result?.Day);
    }

    #endregion

    #region DateTimeToUnix Tests

    [TestMethod]
    public void DateTimeToUnix_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.DateTimeToUnix(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void DateTimeToUnix_WhenEpochProvided_ShouldReturnZero()
    {
        var result = Library.DateTimeToUnix(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public void DateTimeToUnix_And_UnixToDateTime_ShouldBeReversible()
    {
        var original = new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var unix = Library.DateTimeToUnix(original);
        var result = Library.UnixToDateTime(unix);

        Assert.AreEqual(original.Year, result?.Year);
        Assert.AreEqual(original.Month, result?.Month);
        Assert.AreEqual(original.Day, result?.Day);
        Assert.AreEqual(original.Hour, result?.Hour);
        Assert.AreEqual(original.Minute, result?.Minute);
        Assert.AreEqual(original.Second, result?.Second);
    }

    #endregion

    #region DateTimeToUnixMillis Tests

    [TestMethod]
    public void DateTimeToUnixMillis_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.DateTimeToUnixMillis(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void DateTimeToUnixMillis_WhenEpochProvided_ShouldReturnZero()
    {
        var result = Library.DateTimeToUnixMillis(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(0L, result);
    }

    #endregion
}
