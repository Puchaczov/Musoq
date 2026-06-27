using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseStrictConversionsDecimalTests
{
    #region TryConvertToDecimalStrict Tests

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringDecimal_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict("100,50");

        Assert.IsNotNull(result, "Result should not be null");
        Assert.AreEqual(100.50m, result.Value, "Should parse 100,50 correctly");
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringDecimal_MatchesLiteral()
    {
        var result = Library.TryConvertToDecimalStrict("100,50");
        var literal = 100.50m;

        Assert.IsNotNull(result);
        Assert.AreEqual(literal, result.Value, "Parsed value should match literal");
        Assert.AreEqual(literal, result.Value, "Equality comparison should work");
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringInteger_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict("100");

        Assert.IsNotNull(result);
        Assert.AreEqual(100m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithInt_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithLong_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(9223372036854775807L);

        Assert.IsNotNull(result);
        Assert.AreEqual(9223372036854775807m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDouble_WhenExact_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(123.0);

        Assert.IsNotNull(result);
        Assert.AreEqual(123m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithFloat_WhenExact_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(42.0f);

        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithByte_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((byte)255);

        Assert.IsNotNull(result);
        Assert.AreEqual(255m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithSByte_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((sbyte)-128);

        Assert.IsNotNull(result);
        Assert.AreEqual(-128m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithShort_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((short)12345);

        Assert.IsNotNull(result);
        Assert.AreEqual(12345m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithUShort_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((ushort)65535);

        Assert.IsNotNull(result);
        Assert.AreEqual(65535m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithUInt_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(4294967295U);

        Assert.IsNotNull(result);
        Assert.AreEqual(4294967295m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithULong_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(18446744073709551615UL);

        Assert.IsNotNull(result);
        Assert.AreEqual(18446744073709551615m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithBool_True_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(true);

        Assert.IsNotNull(result);
        Assert.AreEqual(1m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithBool_False_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(false);

        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDecimal_ShouldReturnSame()
    {
        var result = Library.TryConvertToDecimalStrict(123.456m);

        Assert.IsNotNull(result);
        Assert.AreEqual(123.456m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.NaN);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDoubleInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.PositiveInfinity);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithNegativeDoubleInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.NegativeInfinity);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithFloatNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(float.NaN);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithFloatInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(float.PositiveInfinity);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithInvalidString_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict("not a number");

        Assert.IsNull(result);
    }

    #endregion
}
