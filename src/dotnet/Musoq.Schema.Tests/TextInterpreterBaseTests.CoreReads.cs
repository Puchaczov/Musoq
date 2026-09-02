using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class TextInterpreterBaseTests
{
    #region ApplyModifiers Tests

    [TestMethod]
    public void ApplyModifiers_NoModifiers_ReturnsSame()
    {
        var result = TestTextInterpreter.TestApplyModifiers("  test  ");
        Assert.AreEqual("  test  ", result);
    }

    [TestMethod]
    public void ApplyModifiers_Ltrim_TrimsStart()
    {
        var result = TestTextInterpreter.TestApplyModifiers("  test  ", true);
        Assert.AreEqual("test  ", result);
    }

    [TestMethod]
    public void ApplyModifiers_Rtrim_TrimsEnd()
    {
        var result = TestTextInterpreter.TestApplyModifiers("  test  ", rtrim: true);
        Assert.AreEqual("  test", result);
    }

    [TestMethod]
    public void ApplyModifiers_BothTrim_TrimsBoth()
    {
        var result = TestTextInterpreter.TestApplyModifiers("  test  ", true, true);
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void ApplyModifiers_Lower_ConvertsToLower()
    {
        var result = TestTextInterpreter.TestApplyModifiers("TEST", lower: true);
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void ApplyModifiers_Upper_ConvertsToUpper()
    {
        var result = TestTextInterpreter.TestApplyModifiers("test", upper: true);
        Assert.AreEqual("TEST", result);
    }

    [TestMethod]
    public void ApplyModifiers_LowerAndUpper_LowerWins()
    {
        var result = TestTextInterpreter.TestApplyModifiers("TEST", lower: true, upper: true);
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void ApplyModifiers_TrimAndLower_BothApply()
    {
        var result = TestTextInterpreter.TestApplyModifiers("  TEST  ", true, true, true);
        Assert.AreEqual("test", result);
    }

    #endregion

    #region ReadUntil Tests

    [TestMethod]
    public void ReadUntil_ValidDelimiter_ReadsUntilDelimiter()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadUntil("hello,world".AsSpan(), ",");
        Assert.AreEqual("hello", result);
        Assert.AreEqual(6, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadUntil_WithTrim_TrimsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadUntil("  hello  ,world".AsSpan(), ",", true);
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void ReadUntil_NotConsumeDelimiter_LeavesDelimiter()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadUntil("hello,world".AsSpan(), ",", consumeDelimiter: false);
        Assert.AreEqual("hello", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadUntil_DelimiterNotFound_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadUntil("hello world".AsSpan(), ","));
    }

    [TestMethod]
    public void ReadUntil_MultiCharDelimiter_Works()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadUntil("hello::world".AsSpan(), "::");
        Assert.AreEqual("hello", result);
        Assert.AreEqual(7, interpreter.GetPosition());
    }

    #endregion

    #region ReadBetween Tests

    [TestMethod]
    public void ReadBetween_Simple_ReadsBetweenDelimiters()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("[content]".AsSpan(), "[", "]");
        Assert.AreEqual("content", result);
        Assert.AreEqual(9, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadBetween_WithTrim_TrimsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("[  content  ]".AsSpan(), "[", "]", trim: true);
        Assert.AreEqual("content", result);
    }

    [TestMethod]
    public void ReadBetween_Nested_HandlesNesting()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("[[inner]]".AsSpan(), "[", "]", true);
        Assert.AreEqual("[inner]", result);
    }

    [TestMethod]
    public void ReadBetween_MissingOpen_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBetween("content]".AsSpan(), "[", "]"));
    }

    [TestMethod]
    public void ReadBetween_MissingClose_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBetween("[content".AsSpan(), "[", "]"));
    }

    [TestMethod]
    public void ReadBetween_NestedMissingClose_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBetween("[[content]".AsSpan(), "[", "]", true));
    }

    [TestMethod]
    public void ReadBetween_MultiCharDelimiters_Works()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("<<content>>".AsSpan(), "<<", ">>");
        Assert.AreEqual("content", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_IgnoresEscapedCloseDelimiter()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("[content\\]more]".AsSpan(), "[", "]", escaped: true);
        Assert.AreEqual("content\\]more", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_SingleBackslashBeforeClose_IsEscaped()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("[test\\]]".AsSpan(), "[", "]", escaped: true);
        Assert.AreEqual("test\\]", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_EvenBackslashesBeforeClose_NotEscaped()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadBetween("[test\\\\]".AsSpan(), "[", "]", escaped: true);
        Assert.AreEqual("test\\", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_OddBackslashesBeforeClose_IsEscaped()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadBetween("[test\\\\\\]end]".AsSpan(), "[", "]", escaped: true);
        Assert.AreEqual("test\\\\]end", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_MultiCharCloseDelimiter_Works()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("<<content\\>>more>>".AsSpan(), "<<", ">>", escaped: true);
        Assert.AreEqual("content\\>>more", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_NoEscapes_ReadsNormally()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween("[simple]".AsSpan(), "[", "]", escaped: true);
        Assert.AreEqual("simple", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_MissingClose_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBetween("[content\\]".AsSpan(), "[", "]", escaped: true));
    }

    #endregion

    #region ReadChars Tests

    [TestMethod]
    public void ReadChars_ValidCount_ReadsChars()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadChars("hello world".AsSpan(), 5);
        Assert.AreEqual("hello", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadChars_WithTrim_TrimsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadChars("  hello  ".AsSpan(), 7, true);
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void ReadChars_WithLtrim_TrimStartsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadChars("  hello  ".AsSpan(), 7, ltrim: true);
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void ReadChars_WithRtrim_TrimEndsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadChars("  hello  ".AsSpan(), 8, rtrim: true);
        Assert.AreEqual("  hello", result);
    }

    [TestMethod]
    public void ReadChars_NegativeCount_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadChars("hello".AsSpan(), -1));
    }

    [TestMethod]
    public void ReadChars_ExceedsLength_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadChars("hi".AsSpan(), 10));
    }

    #endregion
}
