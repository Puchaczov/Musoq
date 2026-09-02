using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class DiagnosticText050TextReaderTests
{
    [TestMethod]
    public void ReadBetween_Escaped_CustomDoubledEscape_ShouldDecodeEscapeCharacter()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadBetween(
            "[a~~]tail".AsSpan(), "[", "]", escaped: true, escapeCharacter: "~", fieldName: "Value");

        Assert.AreEqual("a~", result);
        Assert.AreEqual(5, interpreter.Position);
    }

    [TestMethod]
    public void ReadBetween_Escaped_OverlappingMultiCharacterClose_ShouldNotSkipUnescapedClose()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadBetween(@"<<a\>>>tail>>".AsSpan(), "<<", ">>", escaped: true,
            fieldName: "Value");

        Assert.AreEqual(@"a\>", result);
        Assert.AreEqual("tail>>", interpreter.TestReadRest(@"<<a\>>>tail>>".AsSpan()));
    }

    [TestMethod]
    public void ReadBetween_Nested_MultiCharacterDelimiters_ShouldBalanceContent()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadBetween("<<a<<b>>c>>tail".AsSpan(), "<<", ">>", nested: true,
            fieldName: "Value");

        Assert.AreEqual("a<<b>>c", result);
        Assert.AreEqual("tail", interpreter.TestReadRest("<<a<<b>>c>>tail".AsSpan()));
    }

    [TestMethod]
    public void ReadBetween_Nested_WhenDelimitersAreEqual_ShouldUseFirstClosingDelimiter()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadBetween("\"value\"tail".AsSpan(), "\"", "\"", nested: true,
            fieldName: "Value");

        Assert.AreEqual("value", result);
        Assert.AreEqual(7, interpreter.Position);
    }

    [TestMethod]
    public void ReadBetween_Nested_MissingClose_ShouldReportDelimiterErrorAtNestedContent()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadBetween("[[value]".AsSpan(), "[", "]", nested: true, fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.DelimiterNotFound, exception.ErrorCode);
        Assert.AreEqual("Text050", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(1, exception.Position);
    }

    [TestMethod]
    public void ReadUntil_EmptyDelimiter_ShouldReportStructuredGeneralError()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadUntil("value".AsSpan(), string.Empty, fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.GeneralError, exception.ErrorCode);
        Assert.AreEqual("Text050", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        StringAssert.Contains(exception.Details, "non-empty delimiter");
    }

    [TestMethod]
    public void ReadPattern_UnicodeLetters_ShouldCaptureCharactersAndAdvanceByCharCount()
    {
        var interpreter = new TestTextInterpreter();

        var result = interpreter.TestReadPattern("Łódź!".AsSpan(), @"\p{L}+", fieldName: "Word");

        Assert.AreEqual("Łódź", result);
        Assert.AreEqual(4, interpreter.Position);
    }

    [TestMethod]
    public void ReadPattern_AnchorAtOffset_ShouldRemainCurrentPositionAnchored()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(2);

        var result = interpreter.TestReadPattern("xx123!".AsSpan(), @"^\d+", fieldName: "Digits");

        Assert.AreEqual("123", result);
        Assert.AreEqual(5, interpreter.Position);
    }

    [TestMethod]
    public void LookaheadPattern_LeadingContiguousAnchor_ShouldRemainSupported()
    {
        var interpreter = new TestTextInterpreter();

        Assert.IsTrue(interpreter.TestLookaheadMatchesPattern("123tail".AsSpan(), @"\G\d+", "Value"));
        Assert.AreEqual(0, interpreter.Position);
    }

    [TestMethod]
    public void ReadPattern_BoundedQuantifier_ShouldHonorLazyAndGreedyModes()
    {
        var greedy = new TestTextInterpreter();
        var lazy = new TestTextInterpreter();

        Assert.AreEqual("123", greedy.TestReadPattern("12345".AsSpan(), @"\d{1,3}", greedy: true));
        Assert.AreEqual("1", lazy.TestReadPattern("12345".AsSpan(), @"\d{1,3}", lazy: true));
    }

    [TestMethod]
    public void ReadPattern_UnsupportedConstruct_ShouldReportStableErrorAtCurrentPosition()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(2);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadPattern("xxabc".AsSpan(), @"(?=a)abc", fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.GeneralError, exception.ErrorCode);
        Assert.AreEqual("Text050", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(2, exception.Position);
        StringAssert.Contains(exception.Details, "unsupported construct");
    }

    [TestMethod]
    public void ReadPattern_MalformedConstruct_ShouldReportStableErrorAtCurrentPosition()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadPattern("value".AsSpan(), "[", fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.GeneralError, exception.ErrorCode);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        StringAssert.Contains(exception.Details, "invalid");
    }

    [TestMethod]
    public void ReadUntil_MissingDelimiterAfterPrefix_ShouldReportPrefixOffsetAndField()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(2);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadUntil("xxvalue".AsSpan(), ":", fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.DelimiterNotFound, exception.ErrorCode);
        Assert.AreEqual("Text050", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(2, exception.Position);
    }

    [TestMethod]
    public void ReadBetween_EmptyDelimiter_ShouldReportStructuredGeneralError()
    {
        var interpreter = new TestTextInterpreter();

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadBetween("value".AsSpan(), string.Empty, "]", fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.GeneralError, exception.ErrorCode);
        Assert.AreEqual("Text050", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        StringAssert.Contains(exception.Details, "non-empty");
    }

    [TestMethod]
    public void ReadBetween_MissingCloseAfterPrefix_ShouldReportPositionAfterOpeningDelimiter()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(2);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestReadBetween("xx[value".AsSpan(), "[", "]", fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.DelimiterNotFound, exception.ErrorCode);
        Assert.AreEqual("Text050", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(3, exception.Position);
    }

    [TestMethod]
    public void SkipWhitespace_RequiredAfterPrefix_ShouldReportCurrentOffsetAndField()
    {
        var interpreter = new TestTextInterpreter();
        interpreter.SetPosition(2);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            interpreter.TestSkipWhitespace("xxvalue".AsSpan(), required: true, fieldName: "Gap"));

        Assert.AreEqual(ParseErrorCode.ExpectedWhitespace, exception.ErrorCode);
        Assert.AreEqual("Text050", exception.SchemaName);
        Assert.AreEqual("Gap", exception.FieldName);
        Assert.AreEqual(2, exception.Position);
    }

    [TestMethod]
    public void TextReaders_InvalidCursor_ShouldUseInvalidPositionDiagnostic()
    {
        var negative = new TestTextInterpreter();
        negative.SetPosition(-1);
        var negativeException = Assert.ThrowsExactly<ParseException>(() =>
            negative.TestReadRest("value".AsSpan(), fieldName: "Value"));

        var pastEnd = new TestTextInterpreter();
        pastEnd.SetPosition(6);
        var pastEndException = Assert.ThrowsExactly<ParseException>(() =>
            pastEnd.TestReadToken("value".AsSpan(), fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.InvalidPosition, negativeException.ErrorCode);
        Assert.AreEqual(-1, negativeException.Position);
        Assert.AreEqual("Value", negativeException.FieldName);
        Assert.AreEqual(ParseErrorCode.InvalidPosition, pastEndException.ErrorCode);
        Assert.AreEqual(6, pastEndException.Position);
        Assert.AreEqual("Value", pastEndException.FieldName);
    }

    private sealed class TestTextInterpreter : TextInterpreterBase<string>
    {
        public override string SchemaName => "Text050";

        public override string ParseAt(ReadOnlySpan<char> text, int position)
        {
            ParsePosition = position;
            return ReadRest(text);
        }

        public string TestReadBetween(ReadOnlySpan<char> text, string open, string close, bool nested = false,
            bool trim = false, bool escaped = false, string? escapeCharacter = null, string? fieldName = null)
        {
            return ReadBetween(text, open, close, nested, trim, escaped, escapeCharacter, fieldName);
        }

        public string TestReadPattern(ReadOnlySpan<char> text, string pattern, string? fieldName = null,
            bool greedy = false, bool lazy = false)
        {
            return ReadPattern(text, pattern, fieldName: fieldName, greedy: greedy, lazy: lazy);
        }

        public string TestReadUntil(ReadOnlySpan<char> text, string delimiter, string? fieldName = null)
        {
            return ReadUntil(text, delimiter, fieldName: fieldName);
        }

        public string TestReadRest(ReadOnlySpan<char> text, string? fieldName = null)
        {
            return ReadRest(text, fieldName: fieldName);
        }

        public string TestReadToken(ReadOnlySpan<char> text, string? fieldName = null)
        {
            return ReadToken(text, fieldName: fieldName);
        }

        public void TestSkipWhitespace(ReadOnlySpan<char> text, bool required, string? fieldName)
        {
            SkipWhitespace(text, required, fieldName);
        }

        public bool TestLookaheadMatchesPattern(ReadOnlySpan<char> text, string pattern, string fieldName)
        {
            return LookaheadMatchesPattern(text, pattern, fieldName);
        }

        public void SetPosition(int position)
        {
            ParsePosition = position;
        }
    }
}
