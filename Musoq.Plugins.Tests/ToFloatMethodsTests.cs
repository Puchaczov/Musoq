using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ToFloatMethodsTests : LibraryBaseBaseTests
{
    #region String ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenStringProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat("42");

        Assert.IsNotNull(result);
        Assert.AreEqual(42.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullStringProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((string?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenInvalidStringProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat("not a number");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenNegativeStringProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat("-12345");

        Assert.IsNotNull(result);
        Assert.AreEqual(-12345.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenEmptyStringProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenIntegerStringProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat("42");

        Assert.AreEqual(42.0f, result);
    }

    #endregion

    #region Byte ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenByteProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((byte)100);

        Assert.AreEqual(100.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullByteProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((byte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenByteMinValueProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(byte.MinValue);

        Assert.AreEqual(0.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenByteMaxValueProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(byte.MaxValue);

        Assert.AreEqual(255.0f, result);
    }

    #endregion

    #region SByte ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenSByteProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((sbyte)100);

        Assert.AreEqual(100.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullSByteProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((sbyte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenNegativeSByteProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((sbyte)-50);

        Assert.AreEqual(-50.0f, result);
    }

    #endregion

    #region Short ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenShortProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((short)1000);

        Assert.AreEqual(1000.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullShortProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((short?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenNegativeShortProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((short)-1000);

        Assert.AreEqual(-1000.0f, result);
    }

    #endregion

    #region UShort ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenUShortProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((ushort)50000);

        Assert.AreEqual(50000.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullUShortProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((ushort?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenUShortMaxValueProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(ushort.MaxValue);

        Assert.AreEqual(65535.0f, result);
    }

    #endregion

    #region Int ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenIntProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(12345);

        Assert.AreEqual(12345.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullIntProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((int?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenNegativeIntProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(-12345);

        Assert.AreEqual(-12345.0f, result);
    }

    #endregion

    #region UInt ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenUIntProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((uint)4000000);

        Assert.AreEqual(4000000.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullUIntProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((uint?)null);

        Assert.IsNull(result);
    }

    #endregion

    #region Long ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenLongProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(123456L);

        Assert.AreEqual(123456.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullLongProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((long?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenNegativeLongProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(-123456L);

        Assert.AreEqual(-123456.0f, result);
    }

    #endregion

    #region ULong ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenULongProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat((ulong)123456);

        Assert.AreEqual(123456.0f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullULongProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((ulong?)null);

        Assert.IsNull(result);
    }

    #endregion

    #region Float ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenFloatProvided_ShouldReturnSameFloat()
    {
        var result = Library.ToFloat(3.14f);

        Assert.AreEqual(3.14f, result);
    }

    [TestMethod]
    public void ToFloat_WhenNullFloatProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((float?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenFloatMaxValueProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(float.MaxValue);

        Assert.AreEqual(float.MaxValue, result);
    }

    [TestMethod]
    public void ToFloat_WhenNegativeFloatProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(-3.14f);

        Assert.AreEqual(-3.14f, result);
    }

    #endregion

    #region Decimal ToFloat Tests

    [TestMethod]
    public void ToFloat_WhenDecimalProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(123.456m);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.01f, Math.Abs(result.Value - 123.456f));
    }

    [TestMethod]
    public void ToFloat_WhenNullDecimalProvided_ShouldReturnNull()
    {
        var result = Library.ToFloat((decimal?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToFloat_WhenNegativeDecimalProvided_ShouldReturnFloat()
    {
        var result = Library.ToFloat(-123.456m);

        Assert.IsNotNull(result);
        Assert.IsLessThan(0, result.Value);
    }

    #endregion
}
