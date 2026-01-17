using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ToDoubleMethodsTests : LibraryBaseBaseTests
{
    #region Object ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenObjectIntProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((object)42);

        Assert.AreEqual(42.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullObjectProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((object?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenObjectDoubleProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((object)3.14);

        Assert.AreEqual(3.14, result);
    }

    [TestMethod]
    public void ToDouble_WhenObjectStringNumericProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((object)12345);

        Assert.AreEqual(12345.0, result);
    }

    #endregion

    #region String ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenStringProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble("42");

        Assert.IsNotNull(result);
        Assert.AreEqual(42.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullStringProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((string?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenInvalidStringProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble("not a number");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenNegativeStringProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble("-12345");

        Assert.IsNotNull(result);
        Assert.AreEqual(-12345.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenEmptyStringProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenIntegerStringProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble("42");

        Assert.AreEqual(42.0, result);
    }

    #endregion

    #region Byte ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenByteProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((byte)100);

        Assert.AreEqual(100.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullByteProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((byte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenByteMinValueProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(byte.MinValue);

        Assert.AreEqual(0.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenByteMaxValueProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(byte.MaxValue);

        Assert.AreEqual(255.0, result);
    }

    #endregion

    #region SByte ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenSByteProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((sbyte)100);

        Assert.AreEqual(100.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullSByteProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((sbyte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenNegativeSByteProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((sbyte)-50);

        Assert.AreEqual(-50.0, result);
    }

    #endregion

    #region Short ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenShortProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((short)1000);

        Assert.AreEqual(1000.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullShortProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((short?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenNegativeShortProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((short)-1000);

        Assert.AreEqual(-1000.0, result);
    }

    #endregion

    #region UShort ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenUShortProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((ushort)50000);

        Assert.AreEqual(50000.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullUShortProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((ushort?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenUShortMaxValueProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(ushort.MaxValue);

        Assert.AreEqual(65535.0, result);
    }

    #endregion

    #region Int ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenIntProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(12345);

        Assert.AreEqual(12345.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullIntProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((int?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenNegativeIntProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(-12345);

        Assert.AreEqual(-12345.0, result);
    }

    #endregion

    #region UInt ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenUIntProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((uint)4000000000);

        Assert.AreEqual(4000000000.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullUIntProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((uint?)null);

        Assert.IsNull(result);
    }

    #endregion

    #region Long ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenLongProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(123456789012L);

        Assert.AreEqual(123456789012.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullLongProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((long?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenNegativeLongProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(-123456789012L);

        Assert.AreEqual(-123456789012.0, result);
    }

    #endregion

    #region ULong ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenULongProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble((ulong)123456789012);

        Assert.AreEqual(123456789012.0, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullULongProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((ulong?)null);

        Assert.IsNull(result);
    }

    #endregion

    #region Float ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenFloatProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(3.14f);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.01, Math.Abs(result.Value - 3.14));
    }

    [TestMethod]
    public void ToDouble_WhenNullFloatProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((float?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenNegativeFloatProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(-3.14f);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0, result.Value);
    }

    #endregion

    #region Double ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenDoubleProvided_ShouldReturnSameDouble()
    {
        var result = Library.ToDouble(3.14159265359);

        Assert.AreEqual(3.14159265359, result);
    }

    [TestMethod]
    public void ToDouble_WhenNullDoubleProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((double?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenDoubleMaxValueProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(double.MaxValue);

        Assert.AreEqual(double.MaxValue, result);
    }

    #endregion

    #region Decimal ToDouble Tests

    [TestMethod]
    public void ToDouble_WhenDecimalProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(123.456m);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.001, Math.Abs(result.Value - 123.456));
    }

    [TestMethod]
    public void ToDouble_WhenNullDecimalProvided_ShouldReturnNull()
    {
        var result = Library.ToDouble((decimal?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDouble_WhenNegativeDecimalProvided_ShouldReturnDouble()
    {
        var result = Library.ToDouble(-123.456m);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0, result.Value);
    }

    #endregion
}
