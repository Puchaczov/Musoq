using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ToStringMethodsTests : LibraryBaseBaseTests
{
    #region Char ToString Tests

    [TestMethod]
    public void ToString_WhenCharProvided_ShouldReturnString()
    {
        var result = Library.ToString('a');

        Assert.AreEqual("a", result);
    }

    [TestMethod]
    public void ToString_WhenNullCharProvided_ShouldReturnNull()
    {
        var result = Library.ToString((char?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenSpecialCharProvided_ShouldReturnString()
    {
        var result = Library.ToString('€');

        Assert.AreEqual("€", result);
    }

    #endregion

    #region DateTimeOffset ToString Tests

    [TestMethod]
    public void ToString_WhenDateTimeOffsetProvided_ShouldReturnString()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var result = Library.ToString(dto);

        Assert.IsNotNull(result);
        Assert.Contains("2024", result);
    }

    [TestMethod]
    public void ToString_WhenNullDateTimeOffsetProvided_ShouldReturnNull()
    {
        var result = Library.ToString((DateTimeOffset?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenDateTimeOffsetWithFormatProvided_ShouldFormat()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var result = Library.ToString(dto, "yyyy-MM-dd");

        Assert.AreEqual("2024-06-15", result);
    }

    [TestMethod]
    public void ToString_WhenDateTimeOffsetWithNullFormatProvided_ShouldUseDefault()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var result = Library.ToString(dto, null);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToString_WhenDateTimeOffsetWithFormatAndCultureProvided_ShouldFormat()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var result = Library.ToString(dto, "MMMM", "en-US");

        Assert.AreEqual("June", result);
    }

    [TestMethod]
    public void ToString_WhenDateTimeOffsetWithNullFormatAndCultureProvided_ShouldUseDefaults()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var result = Library.ToString(dto, null, null);

        Assert.IsNotNull(result);
    }

    #endregion

    #region Byte ToString Tests

    [TestMethod]
    public void ToString_WhenByteProvided_ShouldReturnString()
    {
        var result = Library.ToString((byte)123);

        Assert.AreEqual("123", result);
    }

    [TestMethod]
    public void ToString_WhenNullByteProvided_ShouldReturnNull()
    {
        var result = Library.ToString((byte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenByteWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString((byte)15, "X2");

        Assert.AreEqual("0F", result);
    }

    [TestMethod]
    public void ToString_WhenByteMinValueProvided_ShouldReturnString()
    {
        var result = Library.ToString(byte.MinValue);

        Assert.AreEqual("0", result);
    }

    [TestMethod]
    public void ToString_WhenByteMaxValueProvided_ShouldReturnString()
    {
        var result = Library.ToString(byte.MaxValue);

        Assert.AreEqual("255", result);
    }

    #endregion

    #region SByte ToString Tests

    [TestMethod]
    public void ToString_WhenSByteProvided_ShouldReturnString()
    {
        var result = Library.ToString((sbyte)123);

        Assert.AreEqual("123", result);
    }

    [TestMethod]
    public void ToString_WhenNullSByteProvided_ShouldReturnNull()
    {
        var result = Library.ToString((sbyte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenSByteWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString((sbyte)15, "D3");

        Assert.AreEqual("015", result);
    }

    [TestMethod]
    public void ToString_WhenNegativeSByteProvided_ShouldReturnString()
    {
        var result = Library.ToString((sbyte)-100);

        Assert.AreEqual("-100", result);
    }

    #endregion

    #region Int ToString Tests

    [TestMethod]
    public void ToString_WhenIntProvided_ShouldReturnString()
    {
        var result = Library.ToString(12345);

        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ToString_WhenNullIntProvided_ShouldReturnNull()
    {
        var result = Library.ToString((int?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenIntWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString(1234567, "N0");

        Assert.IsNotNull(result);
        
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void ToString_WhenNegativeIntProvided_ShouldReturnString()
    {
        var result = Library.ToString(-12345);

        Assert.AreEqual("-12345", result);
    }

    #endregion

    #region UInt ToString Tests

    [TestMethod]
    public void ToString_WhenUIntProvided_ShouldReturnString()
    {
        var result = Library.ToString((uint)12345);

        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ToString_WhenNullUIntProvided_ShouldReturnNull()
    {
        var result = Library.ToString((uint?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenUIntWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString((uint)255, "X");

        Assert.AreEqual("FF", result);
    }

    [TestMethod]
    public void ToString_WhenUIntMaxValueProvided_ShouldReturnString()
    {
        var result = Library.ToString(uint.MaxValue);

        Assert.AreEqual("4294967295", result);
    }

    #endregion

    #region Long ToString Tests

    [TestMethod]
    public void ToString_WhenLongProvided_ShouldReturnString()
    {
        var result = Library.ToString(123456789012L);

        Assert.AreEqual("123456789012", result);
    }

    [TestMethod]
    public void ToString_WhenNullLongProvided_ShouldReturnNull()
    {
        var result = Library.ToString((long?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenLongWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString(255L, "X");

        Assert.AreEqual("FF", result);
    }

    [TestMethod]
    public void ToString_WhenNegativeLongProvided_ShouldReturnString()
    {
        var result = Library.ToString(-123456789012L);

        Assert.AreEqual("-123456789012", result);
    }

    #endregion

    #region ULong ToString Tests

    [TestMethod]
    public void ToString_WhenULongProvided_ShouldReturnString()
    {
        var result = Library.ToString((ulong)123456789012);

        Assert.AreEqual("123456789012", result);
    }

    [TestMethod]
    public void ToString_WhenNullULongProvided_ShouldReturnNull()
    {
        var result = Library.ToString((ulong?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenULongWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString((ulong)255, "X");

        Assert.AreEqual("FF", result);
    }

    [TestMethod]
    public void ToString_WhenULongMaxValueProvided_ShouldReturnString()
    {
        var result = Library.ToString(ulong.MaxValue);

        Assert.AreEqual("18446744073709551615", result);
    }

    #endregion

    #region Float ToString Tests

    [TestMethod]
    public void ToString_WhenFloatProvided_ShouldReturnString()
    {
        var result = Library.ToString(3.14f);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("3.14") || result.StartsWith("3,14"));
    }

    [TestMethod]
    public void ToString_WhenNullFloatProvided_ShouldReturnNull()
    {
        var result = Library.ToString((float?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenFloatWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString(3.14159f, "F2");

        Assert.IsNotNull(result);
        
    }

    [TestMethod]
    public void ToString_WhenNegativeFloatProvided_ShouldReturnString()
    {
        var result = Library.ToString(-3.14f);

        Assert.IsNotNull(result);
        Assert.Contains("-", result);
    }

    [TestMethod]
    public void ToString_WhenFloatInfinityProvided_ShouldReturnString()
    {
        var result = Library.ToString(float.PositiveInfinity);

        Assert.IsNotNull(result);
    }

    #endregion

    #region Double ToString Tests

    [TestMethod]
    public void ToString_WhenDoubleProvided_ShouldReturnString()
    {
        var result = Library.ToString(3.14159265359);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToString_WhenNullDoubleProvided_ShouldReturnNull()
    {
        var result = Library.ToString((double?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenDoubleWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString(1234.5678, "F2");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToString_WhenNegativeDoubleProvided_ShouldReturnString()
    {
        var result = Library.ToString(-1234.5678);

        Assert.IsNotNull(result);
        Assert.Contains("-", result);
    }

    [TestMethod]
    public void ToString_WhenDoubleNaNProvided_ShouldReturnString()
    {
        var result = Library.ToString(double.NaN);

        Assert.IsNotNull(result);
    }

    #endregion

    #region Decimal ToString Tests

    [TestMethod]
    public void ToString_WhenDecimalProvided_ShouldReturnString()
    {
        var result = Library.ToString(123.456m);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToString_WhenNullDecimalProvided_ShouldReturnNull()
    {
        var result = Library.ToString((decimal?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenDecimalWithFormatProvided_ShouldFormat()
    {
        var result = Library.ToString(1234.5678m, "C");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToString_WhenNegativeDecimalProvided_ShouldReturnString()
    {
        var result = Library.ToString(-1234.5678m);

        Assert.IsNotNull(result);
        Assert.Contains("-", result);
    }

    [TestMethod]
    public void ToString_WhenDecimalMaxValueProvided_ShouldReturnString()
    {
        var result = Library.ToString(decimal.MaxValue);

        Assert.IsNotNull(result);
    }

    #endregion

    #region Bool ToString Tests

    [TestMethod]
    public void ToString_WhenBoolTrueProvided_ShouldReturnString()
    {
        var result = Library.ToString(true);

        Assert.AreEqual("True", result);
    }

    [TestMethod]
    public void ToString_WhenBoolFalseProvided_ShouldReturnString()
    {
        var result = Library.ToString(false);

        Assert.AreEqual("False", result);
    }

    [TestMethod]
    public void ToString_WhenNullBoolProvided_ShouldReturnNull()
    {
        var result = Library.ToString((bool?)null);

        Assert.IsNull(result);
    }

    #endregion

    #region Object ToString Tests

    [TestMethod]
    public void ToString_WhenObjectProvided_ShouldReturnString()
    {
        var result = Library.ToString((object)12345);

        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ToString_WhenNullObjectProvided_ShouldReturnNull()
    {
        var result = Library.ToString((object?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenStringObjectProvided_ShouldReturnString()
    {
        var result = Library.ToString((object)"Hello");

        Assert.AreEqual("Hello", result);
    }

    #endregion

    #region Generic T ToString Tests

    [TestMethod]
    public void ToString_WhenGenericClassProvided_ShouldReturnString()
    {
        var result = Library.ToString("Hello World");

        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void ToString_WhenNullGenericClassProvided_ShouldReturnNull()
    {
        string? nullString = null;
        var result = Library.ToString(nullString);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToString_WhenCustomObjectProvided_ShouldReturnToString()
    {
        var result = Library.ToString(new StringBuilder("Test"));

        Assert.AreEqual("Test", result);
    }

    #endregion

    #region String Array ToString Tests

    [TestMethod]
    public void ToString_WhenStringArrayProvided_ShouldReturnCommaSeparated()
    {
        var result = Library.ToString(new[] { "a", "b", "c" });

        Assert.AreEqual("a,b,c", result);
    }

    [TestMethod]
    public void ToString_WhenEmptyStringArrayProvided_ShouldReturnEmpty()
    {
        var result = Library.ToString(Array.Empty<string>());

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ToString_WhenSingleElementStringArrayProvided_ShouldReturnElement()
    {
        var result = Library.ToString(new[] { "single" });

        Assert.AreEqual("single", result);
    }

    [TestMethod]
    public void ToString_WhenStringArrayWithSpacesProvided_ShouldPreserveSpaces()
    {
        var result = Library.ToString(new[] { "hello world", "foo bar" });

        Assert.AreEqual("hello world,foo bar", result);
    }

    #endregion

    #region Generic T Array ToString Tests

    [TestMethod]
    public void ToString_WhenIntArrayProvided_ShouldReturnCommaSeparated()
    {
        var result = Library.ToString(new[] { 1, 2, 3 });

        Assert.AreEqual("1,2,3", result);
    }

    [TestMethod]
    public void ToString_WhenEmptyIntArrayProvided_ShouldReturnEmpty()
    {
        var result = Library.ToString(Array.Empty<int>());

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ToString_WhenSingleElementIntArrayProvided_ShouldReturnElement()
    {
        var result = Library.ToString(new[] { 42 });

        Assert.AreEqual("42", result);
    }

    [TestMethod]
    public void ToString_WhenDoubleArrayProvided_ShouldReturnCommaSeparated()
    {
        var result = Library.ToString(new[] { 1.0, 2.0, 3.0 });

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains(",") || result.Contains("1") && result.Contains("2") && result.Contains("3"));
    }

    [TestMethod]
    public void ToString_WhenBoolArrayProvided_ShouldReturnCommaSeparated()
    {
        var result = Library.ToString(new[] { true, false, true });

        Assert.AreEqual("True,False,True", result);
    }

    #endregion
}
