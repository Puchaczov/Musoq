using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ToDecimalMethodsTests : LibraryBaseBaseTests
{
    #region String ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenStringProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal("12345");

        Assert.IsNotNull(result);
        Assert.AreEqual(12345m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullStringProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((string?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenInvalidStringProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal("not a number");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenNegativeStringProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal("-12345");

        Assert.IsNotNull(result);
        Assert.AreEqual(-12345m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenEmptyStringProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenIntegerStringProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal("42");

        Assert.AreEqual(42m, result);
    }

    #endregion

    #region String with Culture ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenStringWithCultureProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal("1234.56", "en-US");

        Assert.IsNotNull(result);
        Assert.AreEqual(1234.56m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenInvalidStringWithCultureProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal("not a number", "en-US");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenNegativeStringWithCultureProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal("-1234.56", "en-US");

        Assert.IsNotNull(result);
        Assert.IsLessThan(0, result.Value);
    }

    #endregion

    #region Byte ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenByteProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal((byte)100);

        Assert.AreEqual(100m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullByteProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((byte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenByteMinValueProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(byte.MinValue);

        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenByteMaxValueProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(byte.MaxValue);

        Assert.AreEqual(255m, result);
    }

    #endregion

    #region SByte ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenSByteProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(100);

        Assert.AreEqual(100m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullSByteProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((sbyte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenNegativeSByteProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(-50);

        Assert.AreEqual(-50m, result);
    }

    #endregion

    #region Short ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenShortProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(1000);

        Assert.AreEqual(1000m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullShortProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((short?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenNegativeShortProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(-1000);

        Assert.AreEqual(-1000m, result);
    }

    #endregion

    #region UShort ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenUShortProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(50000);

        Assert.AreEqual(50000m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullUShortProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((ushort?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenUShortMaxValueProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(ushort.MaxValue);

        Assert.AreEqual(65535m, result);
    }

    #endregion

    #region Long ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenLongProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(123456789012L);

        Assert.AreEqual(123456789012m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullLongProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((long?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenNegativeLongProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(-123456789012L);

        Assert.AreEqual(-123456789012m, result);
    }

    #endregion

    #region ULong ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenULongProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(123456789012UL);

        Assert.AreEqual(123456789012m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullULongProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((ulong?)null);

        Assert.IsNull(result);
    }

    #endregion

    #region Float ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenFloatProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(3.14f);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.01m, Math.Abs(result.Value - 3.14m));
    }

    [TestMethod]
    public void ToDecimal_WhenNullFloatProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((float?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenNegativeFloatProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(-3.14f);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0, result.Value);
    }

    #endregion

    #region Double ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenDoubleProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(123.456);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.001m, Math.Abs(result.Value - 123.456m));
    }

    [TestMethod]
    public void ToDecimal_WhenNullDoubleProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((double?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenNegativeDoubleProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(-123.456);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0, result.Value);
    }

    #endregion

    #region Object ToDecimal Tests

    [TestMethod]
    public void ToDecimal_WhenObjectIntProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal((object)42);

        Assert.AreEqual(42m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenNullObjectProvided_ShouldReturnNull()
    {
        var result = Library.ToDecimal((object?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToDecimal_WhenObjectDecimalProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal(123.456m);

        Assert.AreEqual(123.456m, result);
    }

    [TestMethod]
    public void ToDecimal_WhenObjectDoubleProvided_ShouldReturnDecimal()
    {
        var result = Library.ToDecimal((object)3.14);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.01m, Math.Abs(result.Value - 3.14m));
    }

    #endregion
}
