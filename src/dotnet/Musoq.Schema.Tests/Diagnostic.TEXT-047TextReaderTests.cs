using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Schema.Tests;

public partial class TextInterpreterBaseTests
{
    [TestMethod]
    public void ReadBetween_CustomEscape_UsesConfiguredEscapeCharacter()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadBetween("[a~]b]tail".AsSpan(), "[", "]", escaped: true,
            escapeCharacter: "~", fieldName: "Value");

        Assert.AreEqual("a~]b", result);
        Assert.AreEqual(6, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadPattern_Mismatch_ShouldReportFieldAndPosition()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<Musoq.Schema.Interpreters.ParseException>(() =>
            interpreter.TestReadPattern("abc".AsSpan(), @"\d+", fieldName: "Digits"));

        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.PatternMismatch, exception.ErrorCode);
        Assert.AreEqual("Digits", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void ExpectLiteral_Mismatch_ShouldReportFieldAndPosition()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<Musoq.Schema.Interpreters.ParseException>(() =>
            interpreter.TestExpectLiteral("abc".AsSpan(), "def", "Marker"));

        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.LiteralMismatch, exception.ErrorCode);
        Assert.AreEqual("Marker", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void ReadUntil_MissingDelimiter_ShouldReportFieldAndPosition()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<Musoq.Schema.Interpreters.ParseException>(() =>
            interpreter.TestReadUntil("abc".AsSpan(), ":", fieldName: "Value"));

        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.DelimiterNotFound, exception.ErrorCode);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void ReadBetween_MissingOpeningDelimiter_ShouldReportFieldAndPosition()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<Musoq.Schema.Interpreters.ParseException>(() =>
            interpreter.TestReadBetween("abc]".AsSpan(), "[", "]", fieldName: "Value"));

        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.ExpectedDelimiter, exception.ErrorCode);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void ReadPattern_UnsupportedConstruct_ShouldReportGeneralFieldError()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<Musoq.Schema.Interpreters.ParseException>(() =>
            interpreter.TestReadPattern("abc".AsSpan(), "(?=a)abc", fieldName: "Value"));

        Assert.AreEqual(Musoq.Schema.Interpreters.ParseErrorCode.GeneralError, exception.ErrorCode);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }
}
