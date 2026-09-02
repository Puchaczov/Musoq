using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class DiagnosticText048TextReaderTests
{
    [TestMethod]
    public void ReadChars_InsufficientData_ShouldReportFieldAndPosition()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadChars("ab".AsSpan(), 3, fieldName: "Code"));

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual("Code", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void ReadChars_ZeroLength_ShouldBeEmptyAndNotAdvance()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadChars("abc".AsSpan(), 0);

        Assert.AreEqual(string.Empty, result);
        Assert.AreEqual(0, interpreter.Position);
    }

    [TestMethod]
    public void ReadToken_ShouldTreatOnlySpecifiedAsciiWhitespaceAsDelimiter()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadToken("a\u00A0b".AsSpan());

        Assert.AreEqual("a\u00A0b", result);
        Assert.AreEqual(3, interpreter.Position);
    }

    [TestMethod]
    public void SkipWhitespace_Required_ShouldRejectNonSchemaWhitespace()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestSkipWhitespace("\u00A0value".AsSpan(), required: true, fieldName: "Gap"));

        Assert.AreEqual(ParseErrorCode.ExpectedWhitespace, exception.ErrorCode);
        Assert.AreEqual("Gap", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void ReadWhitespace_Quantifiers_ShouldFollowSchemaSemantics()
    {
        var plus = new TestTextInterpreter();
        var star = new TestTextInterpreter();
        var question = new TestTextInterpreter();

        Assert.AreEqual("   ", plus.TestReadWhitespace("   value".AsSpan(), "+"));
        Assert.AreEqual(3, plus.Position);
        Assert.AreEqual(string.Empty, star.TestReadWhitespace("value".AsSpan(), "*"));
        Assert.AreEqual(0, star.Position);
        Assert.AreEqual(" ", question.TestReadWhitespace("  value".AsSpan(), "?"));
        Assert.AreEqual(1, question.Position);
    }

    [TestMethod]
    public void ReadWhitespace_RequiredWithoutInput_ShouldReportExpectedWhitespace()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadWhitespace("value".AsSpan(), "+", fieldName: "Gap"));

        Assert.AreEqual(ParseErrorCode.ExpectedWhitespace, exception.ErrorCode);
        Assert.AreEqual("Gap", exception.FieldName);
    }

    [TestMethod]
    public void ReadUntil_GreedyAndLazy_ShouldChooseLastAndFirstDelimiter()
    {
        var greedy = new TestTextInterpreter();
        var lazy = new TestTextInterpreter();

        Assert.AreEqual("a,b", greedy.TestReadUntil("a,b,c".AsSpan(), ",", greedy: true));
        Assert.AreEqual(4, greedy.Position);
        Assert.AreEqual("a", lazy.TestReadUntil("a,b,c".AsSpan(), ",", lazy: true));
        Assert.AreEqual(2, lazy.Position);
    }

    [TestMethod]
    public void ReadPattern_LazyAndGreedy_ShouldAdjustQuantifierMode()
    {
        var greedy = new TestTextInterpreter();
        var lazy = new TestTextInterpreter();

        Assert.AreEqual("a,b,", greedy.TestReadPattern("a,b,c".AsSpan(), ".*,", greedy: true));
        Assert.AreEqual(4, greedy.Position);
        Assert.AreEqual("a,", lazy.TestReadPattern("a,b,c".AsSpan(), ".*,", lazy: true));
        Assert.AreEqual(2, lazy.Position);
    }

    private sealed class TestTextInterpreter : TextInterpreterBase<string>
    {
        public override string SchemaName => "Text048";

        public override string ParseAt(ReadOnlySpan<char> text, int position)
        {
            ParsePosition = position;
            return ReadRest(text);
        }

        public string TestReadChars(ReadOnlySpan<char> text, int count, string? fieldName = null)
        {
            return ReadChars(text, count, fieldName: fieldName);
        }

        public string TestReadToken(ReadOnlySpan<char> text)
        {
            return ReadToken(text);
        }

        public void TestSkipWhitespace(ReadOnlySpan<char> text, bool required, string? fieldName)
        {
            SkipWhitespace(text, required, fieldName);
        }

        public string TestReadWhitespace(ReadOnlySpan<char> text, string quantifier, string? fieldName = null)
        {
            return ReadWhitespace(text, quantifier, fieldName: fieldName);
        }

        public string TestReadUntil(ReadOnlySpan<char> text, string delimiter, bool greedy = false, bool lazy = false)
        {
            return ReadUntil(text, delimiter, greedy: greedy, lazy: lazy);
        }

        public string TestReadPattern(ReadOnlySpan<char> text, string pattern, bool greedy = false, bool lazy = false)
        {
            return ReadPattern(text, pattern, fieldName: "Value", greedy: greedy, lazy: lazy);
        }
    }
}
