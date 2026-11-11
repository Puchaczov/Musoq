using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NumericOnlyTypeConverterExtendedTests
{
    #region TryConvertToInt64 Tests

    [TestMethod]
    public void TryConvertToInt64_Null_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_LongValue_ReturnsValue()
    {
        long? result = _converter.TryConvertToInt64(9223372036854775807L);
        Assert.AreEqual(9223372036854775807L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ByteValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((byte)255);
        Assert.AreEqual(255L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_SByteValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((sbyte)-128);
        Assert.AreEqual(-128L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ShortValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((short)12345);
        Assert.AreEqual(12345L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_UShortValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((ushort)65535);
        Assert.AreEqual(65535L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_IntValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_UIntValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(3000000000);
        Assert.AreEqual(3000000000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ULongWithinRange_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((ulong)1000);
        Assert.AreEqual(1000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ULongOverMaxValue_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(ulong.MaxValue);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatExactInteger_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42.0f);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatWithFraction_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(42.5f);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatNaN_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatPositiveInfinity_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleExactInteger_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42.0);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleWithFraction_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(42.5);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleNaN_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoublePositiveInfinity_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleNegativeInfinity_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DecimalExactInteger_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42m);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_DecimalWithFraction_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_String_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_Object_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(new object());
        Assert.IsNull(result);
    }

    #endregion
}
