using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class TextInterpreterBaseTests
{
    #region ReadToken Tests

    [TestMethod]
    public void ReadToken_SimpleToken_ReadsUntilWhitespace()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadToken("hello world".AsSpan());
        Assert.AreEqual("hello", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadToken_NoWhitespace_ReadsEntireText()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadToken("hello".AsSpan());
        Assert.AreEqual("hello", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadToken_WithTrim_TrimsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadToken("hello\tworld".AsSpan(), true);
        Assert.AreEqual("hello", result);
    }

    #endregion

    #region ReadRest Tests

    [TestMethod]
    public void ReadRest_Simple_ReadsAllRemaining()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(6);
        var result = interpreter.TestReadRest("hello world".AsSpan());
        Assert.AreEqual("world", result);
        Assert.AreEqual(11, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadRest_WithTrim_TrimsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadRest("  hello  ".AsSpan(), true);
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void ReadRest_WithLtrim_TrimStartsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadRest("  hello  ".AsSpan(), ltrim: true);
        Assert.AreEqual("hello  ", result);
    }

    [TestMethod]
    public void ReadRest_WithRtrim_TrimEndsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadRest("  hello  ".AsSpan(), rtrim: true);
        Assert.AreEqual("  hello", result);
    }

    #endregion

    #region ReadPattern Tests

    [TestMethod]
    public void ReadPattern_ValidPattern_ReadsMatch()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadPattern("12345abc".AsSpan(), @"\d+");
        Assert.AreEqual("12345", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadPattern_WithTrim_TrimsResult()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadPattern("12345  abc".AsSpan(), @"\d+\s*", true);
        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ReadPattern_NoMatch_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadPattern("abc".AsSpan(), @"\d+"));
    }

    [TestMethod]
    public void ReadPattern_AlreadyAnchored_Works()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadPattern("12345abc".AsSpan(), @"\G\d+");
        Assert.AreEqual("12345", result);
    }

    #endregion

    #region SkipWhitespace Tests

    [TestMethod]
    public void SkipWhitespace_HasWhitespace_SkipsIt()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestSkipWhitespace("   hello".AsSpan());
        Assert.AreEqual(3, interpreter.GetPosition());
    }

    [TestMethod]
    public void SkipWhitespace_NoWhitespace_NoChange()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestSkipWhitespace("hello".AsSpan());
        Assert.AreEqual(0, interpreter.GetPosition());
    }

    [TestMethod]
    public void SkipWhitespace_RequiredAndPresent_Succeeds()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestSkipWhitespace(" hello".AsSpan(), true);
        Assert.AreEqual(1, interpreter.GetPosition());
    }

    [TestMethod]
    public void SkipWhitespace_RequiredButMissing_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestSkipWhitespace("hello".AsSpan(), true));
    }

    [TestMethod]
    public void SkipWhitespace_MultipleTabs_SkipsAll()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestSkipWhitespace("\t\t\thello".AsSpan());
        Assert.AreEqual(3, interpreter.GetPosition());
    }

    [TestMethod]
    public void SkipWhitespace_MixedWhitespace_SkipsAll()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestSkipWhitespace(" \t\n\rhello".AsSpan());
        Assert.AreEqual(4, interpreter.GetPosition());
    }

    #endregion

    #region SkipOptionalWhitespace Tests

    [TestMethod]
    public void SkipOptionalWhitespace_HasWhitespace_SkipsOne()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestSkipOptionalWhitespace("  hello".AsSpan());
        Assert.AreEqual(1, interpreter.GetPosition());
    }

    [TestMethod]
    public void SkipOptionalWhitespace_NoWhitespace_NoChange()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestSkipOptionalWhitespace("hello".AsSpan());
        Assert.AreEqual(0, interpreter.GetPosition());
    }

    [TestMethod]
    public void SkipOptionalWhitespace_AtEnd_NoChange()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(5);
        interpreter.TestSkipOptionalWhitespace("hello".AsSpan());
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    #endregion

    #region ExpectLiteral Tests

    [TestMethod]
    public void ExpectLiteral_Matches_ConsumesLiteral()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestExpectLiteral("hello world".AsSpan(), "hello");
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ExpectLiteral_NoMatch_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        var ex = Assert.Throws<ParseException>(() =>
            interpreter.TestExpectLiteral("goodbye".AsSpan(), "hello"));
        Assert.Contains("Expected 'hello'", ex.Message);
    }

    [TestMethod]
    public void ExpectLiteral_TooShort_ThrowsParseException()
    {
        var interpreter = new TestTextInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestExpectLiteral("hi".AsSpan(), "hello"));
    }

    #endregion

    #region IsAtEnd Tests

    [TestMethod]
    public void IsAtEnd_AtEnd_ReturnsTrue()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(5);
        Assert.IsTrue(interpreter.TestIsAtEnd("hello".AsSpan()));
    }

    [TestMethod]
    public void IsAtEnd_PastEnd_ReturnsTrue()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(10);
        Assert.IsTrue(interpreter.TestIsAtEnd("hello".AsSpan()));
    }

    [TestMethod]
    public void IsAtEnd_NotAtEnd_ReturnsFalse()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(3);
        Assert.IsFalse(interpreter.TestIsAtEnd("hello".AsSpan()));
    }

    #endregion

    #region LookaheadMatches Tests

    [TestMethod]
    public void LookaheadMatches_Matches_ReturnsTrue()
    {
        var interpreter = new TestTextInterpreter();
        Assert.IsTrue(interpreter.TestLookaheadMatches("hello".AsSpan(), "hel"));
    }

    [TestMethod]
    public void LookaheadMatches_NoMatch_ReturnsFalse()
    {
        var interpreter = new TestTextInterpreter();
        Assert.IsFalse(interpreter.TestLookaheadMatches("hello".AsSpan(), "xyz"));
    }

    [TestMethod]
    public void LookaheadMatches_TooShort_ReturnsFalse()
    {
        var interpreter = new TestTextInterpreter();
        Assert.IsFalse(interpreter.TestLookaheadMatches("hi".AsSpan(), "hello"));
    }

    [TestMethod]
    public void LookaheadMatches_DoesNotConsumeInput()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestLookaheadMatches("hello".AsSpan(), "hel");
        Assert.AreEqual(0, interpreter.GetPosition());
    }

    #endregion

    #region LookaheadMatchesPattern Tests

    [TestMethod]
    public void LookaheadMatchesPattern_Matches_ReturnsTrue()
    {
        var interpreter = new TestTextInterpreter();
        Assert.IsTrue(interpreter.TestLookaheadMatchesPattern("12345abc".AsSpan(), @"\d+"));
    }

    [TestMethod]
    public void LookaheadMatchesPattern_NoMatch_ReturnsFalse()
    {
        var interpreter = new TestTextInterpreter();
        Assert.IsFalse(interpreter.TestLookaheadMatchesPattern("abc".AsSpan(), @"\d+"));
    }

    [TestMethod]
    public void LookaheadMatchesPattern_WithCaret_Matches()
    {
        var interpreter = new TestTextInterpreter();
        Assert.IsTrue(interpreter.TestLookaheadMatchesPattern("hello".AsSpan(), "^hel"));
    }

    [TestMethod]
    public void LookaheadMatchesPattern_DoesNotConsumeInput()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.TestLookaheadMatchesPattern("12345abc".AsSpan(), @"\d+");
        Assert.AreEqual(0, interpreter.GetPosition());
    }

    #endregion
}
