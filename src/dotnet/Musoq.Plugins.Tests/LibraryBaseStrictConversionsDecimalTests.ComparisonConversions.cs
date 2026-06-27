using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseStrictConversionsDecimalTests
{
    #region TryConvertToInt32Comparison Tests

    [TestMethod]
    public void TryConvertToInt32Comparison_WithDoubleWithFraction_ShouldRound()
    {
        var result = Library.TryConvertToInt32Comparison(42.7);

        Assert.IsNotNull(result);

        Assert.AreEqual(43, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Comparison(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithInt_ShouldReturnSame()
    {
        var result = Library.TryConvertToInt32Comparison(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithLongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Comparison(9223372036854775807L);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithString_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Comparison("12345");

        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.Value);
    }

    #endregion

    #region TryConvertToInt64Comparison Tests

    [TestMethod]
    public void TryConvertToInt64Comparison_WithDoubleWithFraction_ShouldRound()
    {
        var result = Library.TryConvertToInt64Comparison(42.9);

        Assert.IsNotNull(result);

        Assert.AreEqual(43L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Comparison(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithULongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Comparison(18446744073709551615UL);

        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToDecimalComparison Tests

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDouble_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalComparison(123.456);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalComparison(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalComparison(double.NaN);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDoubleInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalComparison(double.PositiveInfinity);

        Assert.IsNull(result);
    }

    #endregion
}
