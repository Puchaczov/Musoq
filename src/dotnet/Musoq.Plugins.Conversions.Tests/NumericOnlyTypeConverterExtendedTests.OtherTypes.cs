using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NumericOnlyTypeConverterExtendedTests
{
    #region Char and Other Types Tests

    [TestMethod]
    public void TryConvertToInt32_CharValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_CharValue_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_CharValue_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_CharValue_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_GuidValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(Guid.NewGuid());
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_TimeSpan_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(TimeSpan.FromDays(1));
        Assert.IsNull(result);
    }

    #endregion
}
