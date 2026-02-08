using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

/// <summary>
///     Tests for TextInterpreterBase helper methods to improve branch coverage.
///     Uses a test-specific interpreter class that exposes protected methods.
/// </summary>
[TestClass]
public class TextInterpreterBaseTests
{
    #region Test Interpreter

    /// <summary>
    ///     Test interpreter that exposes protected methods for testing.
    /// </summary>
    private sealed class TestTextInterpreter : TextInterpreterBase<string>
    {
        public override string SchemaName => "TestSchema";

        public override string ParseAt(ReadOnlySpan<char> text, int position)
        {
            _parsePosition = position;
            return ReadRest(text);
        }

        // Expose protected methods for testing
        public string TestReadUntil(ReadOnlySpan<char> text, string delimiter, bool trim = false,
            bool consumeDelimiter = true)
        {
            return ReadUntil(text, delimiter, trim, consumeDelimiter);
        }

        public string TestReadBetween(ReadOnlySpan<char> text, string open, string close, bool nested = false,
            bool trim = false, bool escaped = false)
        {
            return ReadBetween(text, open, close, nested, trim, escaped);
        }

        public string TestReadChars(ReadOnlySpan<char> text, int count, bool trim = false, bool ltrim = false,
            bool rtrim = false)
        {
            return ReadChars(text, count, trim, ltrim, rtrim);
        }

        public string TestReadToken(ReadOnlySpan<char> text, bool trim = false)
        {
            return ReadToken(text, trim);
        }

        public string TestReadRest(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false)
        {
            return ReadRest(text, trim, ltrim, rtrim);
        }

        public string TestReadPattern(ReadOnlySpan<char> text, string pattern, bool trim = false)
        {
            return ReadPattern(text, pattern, trim);
        }

        public void TestSkipWhitespace(ReadOnlySpan<char> text, bool required = false)
        {
            SkipWhitespace(text, required);
        }

        public void TestSkipOptionalWhitespace(ReadOnlySpan<char> text)
        {
            SkipOptionalWhitespace(text);
        }

        public void TestExpectLiteral(ReadOnlySpan<char> text, string literal)
        {
            ExpectLiteral(text, literal);
        }

        public void TestEnsureChars(ReadOnlySpan<char> text, int count)
        {
            EnsureChars(text, count);
        }

        public bool TestIsAtEnd(ReadOnlySpan<char> text)
        {
            return IsAtEnd(text);
        }

        public bool TestLookaheadMatches(ReadOnlySpan<char> text, string expected)
        {
            return LookaheadMatches(text, expected);
        }

        public bool TestLookaheadMatchesPattern(ReadOnlySpan<char> text, string pattern)
        {
            return LookaheadMatchesPattern(text, pattern);
        }

        public void TestValidate(bool condition, string fieldName, string message)
        {
            Validate(condition, fieldName, message);
        }

        public static string TestApplyModifiers(string value, bool ltrim = false, bool rtrim = false,
            bool lower = false, bool upper = false)
        {
            return ApplyModifiers(value, ltrim, rtrim, lower, upper);
        }

        public int GetPosition()
        {
            return _parsePosition;
        }

        public void SetPosition(int pos)
        {
            _parsePosition = pos;
        }
    }

    #endregion

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
        Assert.AreEqual("test\\\\", result);
    }

    [TestMethod]
    public void ReadBetween_Escaped_OddBackslashesBeforeClose_IsEscaped()
    {
        var interpreter = new TestTextInterpreter();
        
        var result = interpreter.TestReadBetween("[test\\\\\\]end]".AsSpan(), "[", "]", escaped: true);
        Assert.AreEqual("test\\\\\\]end", result);
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
