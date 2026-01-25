using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for LexerException and derived exception classes.
/// </summary>
[TestClass]
public class LexerExceptionTests
{
    #region LexerException Tests

    [TestMethod]
    public void LexerException_Constructor_WithMessage_SetsProperties()
    {
        var ex = new LexerException("Test error", 42);

        Assert.AreEqual("Test error", ex.Message);
        Assert.AreEqual(42, ex.Position);
        Assert.IsNull(ex.InnerException);
    }

    [TestMethod]
    public void LexerException_Constructor_WithInnerException_SetsProperties()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new LexerException("Outer error", 100, inner);

        Assert.AreEqual("Outer error", ex.Message);
        Assert.AreEqual(100, ex.Position);
        Assert.AreSame(inner, ex.InnerException);
    }

    [TestMethod]
    public void LexerException_IsException()
    {
        var ex = new LexerException("Test", 0);
        Assert.IsInstanceOfType<Exception>(ex);
    }

    [TestMethod]
    public void LexerException_Position_CanBeZero()
    {
        var ex = new LexerException("At start", 0);
        Assert.AreEqual(0, ex.Position);
    }

    [TestMethod]
    public void LexerException_Position_CanBeLarge()
    {
        var ex = new LexerException("At end", 10000);
        Assert.AreEqual(10000, ex.Position);
    }

    #endregion

    #region UnknownTokenException Tests

    [TestMethod]
    public void UnknownTokenException_Constructor_SetsAllProperties()
    {
        var ex = new UnknownTokenException(15, '@', "@ SELECT * FROM table");

        Assert.AreEqual('@', ex.Character);
        Assert.AreEqual("@ SELECT * FROM table", ex.RemainingInput);
        Assert.AreEqual(15, ex.Position);
        Assert.Contains("Token '@'", ex.Message);
        Assert.Contains("position 15", ex.Message);
        Assert.Contains("@ SELECT * FROM table", ex.Message);
    }

    [TestMethod]
    public void UnknownTokenException_IsLexerException()
    {
        var ex = new UnknownTokenException(0, '!', "!test");
        Assert.IsInstanceOfType<LexerException>(ex);
    }

    [TestMethod]
    public void UnknownTokenException_WithSpecialCharacter_Works()
    {
        var ex = new UnknownTokenException(5, '\t', "\trest");

        Assert.AreEqual('\t', ex.Character);
        Assert.Contains("Token '\t'", ex.Message);
    }

    [TestMethod]
    public void UnknownTokenException_WithEmptyRemaining_Works()
    {
        var ex = new UnknownTokenException(10, '$', "");

        Assert.AreEqual("", ex.RemainingInput);
        Assert.AreEqual('$', ex.Character);
    }

    [TestMethod]
    public void UnknownTokenException_WithUnicodeCharacter_Works()
    {
        var ex = new UnknownTokenException(0, '€', "€100");

        Assert.AreEqual('€', ex.Character);
        Assert.AreEqual("€100", ex.RemainingInput);
    }

    #endregion
}
