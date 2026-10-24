using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class NullableBase64SliceTests : PluginsTestBase
{
    [TestMethod]
    [DataRow(1, 2, "AgM=")]
    [DataRow(0, 0, "")]
    [DataRow(null, 2, null)]
    [DataRow(1, null, null)]
    [DataRow(null, null, null)]
    public void NullableSlice_ShouldPreserveValuesAndNulls(int? offset, int? length, string? expected)
    {
        Assert.AreEqual(expected, Library.ToBase64([1, 2, 3, 4], offset, length));
    }

    [TestMethod]
    public void NullableSlice_WithNullBytes_ShouldRemainNull()
    {
        Assert.IsNull(Library.ToBase64(null, (int?)0, 1));
    }

    [TestMethod]
    [DataRow(-1, 1)]
    [DataRow(0, -1)]
    [DataRow(3, 2)]
    public void NullableSlice_WithInvalidNonNullBounds_ShouldPreserveRangeValidation(int? offset, int? length)
    {
        Assert.Throws<ArgumentException>(() => Library.ToBase64([1, 2, 3, 4], offset, length));
    }
}
