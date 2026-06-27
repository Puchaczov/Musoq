using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class TextInterpreterBaseTests
{
    #region Validate Tests

    [TestMethod]
    public void Validate_ConditionTrue_NoException()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestValidate(true, "field", "should not throw");
    }

    [TestMethod]
    public void Validate_ConditionFalse_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        var ex = Assert.Throws<ParseException>(() =>
            interpreter.TestValidate(false, "testField", "validation failed"));
        Assert.AreEqual("testField", ex.FieldName);
        Assert.Contains("validation failed", ex.Message);
    }

    #endregion

    #region Parse Interface Tests

    [TestMethod]
    public void Parse_FromSpan_Works()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.Parse("hello world".AsSpan());
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void Parse_FromString_Works()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.Parse("hello world");
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void TryParse_Success_ReturnsTrue()
    {
        var interpreter = new TestTextInterpreter();
        var success = interpreter.TryParse("hello".AsSpan(), out var result);
        Assert.IsTrue(success);
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void CharsConsumed_AfterParse_ReturnsCorrectValue()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.Parse("hello");
        Assert.AreEqual(5, interpreter.CharsConsumed);
    }

    [TestMethod]
    public void Position_AfterParse_ReturnsCorrectValue()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.Parse("hello");
        Assert.AreEqual(5, interpreter.Position);
    }

    #endregion

    #region EnsureChars Tests

    [TestMethod]
    public void EnsureChars_EnoughChars_NoException()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestEnsureChars("hello".AsSpan(), 3);
    }

    [TestMethod]
    public void EnsureChars_NotEnoughChars_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestEnsureChars("hi".AsSpan(), 10));
    }

    #endregion
}
