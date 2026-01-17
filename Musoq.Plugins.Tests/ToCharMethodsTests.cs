using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ToCharMethodsTests : LibraryBaseBaseTests
{
    #region String ToChar Tests

    [TestMethod]
    public void ToChar_WhenStringProvided_ShouldReturnFirstChar()
    {
        var result = Library.ToChar("hello");

        Assert.AreEqual('h', result);
    }

    [TestMethod]
    public void ToChar_WhenNullStringProvided_ShouldReturnNull()
    {
        var result = Library.ToChar((string?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_WhenEmptyStringProvided_ShouldReturnNull()
    {
        var result = Library.ToChar(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_WhenSingleCharStringProvided_ShouldReturnChar()
    {
        var result = Library.ToChar("x");

        Assert.AreEqual('x', result);
    }

    [TestMethod]
    public void ToChar_WhenUnicodeStringProvided_ShouldReturnFirstChar()
    {
        var result = Library.ToChar("€100");

        Assert.AreEqual('€', result);
    }

    [TestMethod]
    public void ToChar_WhenWhitespaceStringProvided_ShouldReturnSpace()
    {
        var result = Library.ToChar(" hello");

        Assert.AreEqual(' ', result);
    }

    #endregion

    #region Int ToChar Tests

    [TestMethod]
    public void ToChar_WhenIntProvided_ShouldReturnChar()
    {
        var result = Library.ToChar(65);

        Assert.AreEqual('A', result);
    }

    [TestMethod]
    public void ToChar_WhenNullIntProvided_ShouldReturnNull()
    {
        var result = Library.ToChar((int?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_WhenZeroIntProvided_ShouldReturnNullChar()
    {
        var result = Library.ToChar(0);

        Assert.AreEqual('\0', result);
    }

    [TestMethod]
    public void ToChar_WhenLowercaseAIntProvided_ShouldReturnLowercaseA()
    {
        var result = Library.ToChar(97);

        Assert.AreEqual('a', result);
    }

    [TestMethod]
    public void ToChar_WhenDigitIntProvided_ShouldReturnDigitChar()
    {
        var result = Library.ToChar(48);

        Assert.AreEqual('0', result);
    }

    #endregion

    #region Short ToChar Tests

    [TestMethod]
    public void ToChar_WhenShortProvided_ShouldReturnChar()
    {
        var result = Library.ToChar((short)66);

        Assert.AreEqual('B', result);
    }

    [TestMethod]
    public void ToChar_WhenNullShortProvided_ShouldReturnNull()
    {
        var result = Library.ToChar((short?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_WhenZeroShortProvided_ShouldReturnNullChar()
    {
        var result = Library.ToChar((short)0);

        Assert.AreEqual('\0', result);
    }

    #endregion

    #region Byte ToChar Tests

    [TestMethod]
    public void ToChar_WhenByteProvided_ShouldReturnChar()
    {
        var result = Library.ToChar(67);

        Assert.AreEqual('C', result);
    }

    [TestMethod]
    public void ToChar_WhenNullByteProvided_ShouldReturnNull()
    {
        var result = Library.ToChar((byte?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_WhenZeroByteProvided_ShouldReturnNullChar()
    {
        var result = Library.ToChar(0);

        Assert.AreEqual('\0', result);
    }

    [TestMethod]
    public void ToChar_WhenMaxByteProvided_ShouldReturnChar()
    {
        var result = Library.ToChar(255);

        Assert.AreEqual((char)255, result);
    }

    #endregion

    #region Object ToChar Tests

    [TestMethod]
    public void ToChar_WhenCharObjectProvided_ShouldReturnChar()
    {
        var result = Library.ToChar((object)'X');

        Assert.AreEqual('X', result);
    }

    [TestMethod]
    public void ToChar_WhenNullObjectProvided_ShouldReturnNull()
    {
        var result = Library.ToChar((object?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToChar_WhenIntObjectProvided_ShouldReturnChar()
    {
        var result = Library.ToChar((object)65);

        Assert.AreEqual('A', result);
    }

    [TestMethod]
    public void ToChar_WhenByteObjectProvided_ShouldReturnChar()
    {
        var result = Library.ToChar((object)(byte)90);

        Assert.AreEqual('Z', result);
    }

    [TestMethod]
    public void ToChar_WhenSingleCharStringObjectProvided_ShouldReturnChar()
    {
        var result = Library.ToChar((object)"Y");

        Assert.AreEqual('Y', result);
    }

    #endregion
}