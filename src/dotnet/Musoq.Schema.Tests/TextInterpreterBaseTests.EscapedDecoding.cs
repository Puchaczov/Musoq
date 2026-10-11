using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class TextInterpreterBaseTests
{
    [TestMethod]
    [DataRow(@"<a\>b>tail", "<", ">", null, "a>b")]
    [DataRow(@"<a\\>tail", "<", ">", null, @"a\")]
    [DataRow(@"<a\\\>b>tail", "<", ">", null, @"a\>b")]
    [DataRow(@"<a\qb>tail", "<", ">", null, @"a\qb")]
    [DataRow(@"<<a\>>b>>tail", "<<", ">>", null, "a>>b")]
    [DataRow(@"<<a\>b>>tail", "<<", ">>", null, @"a\>b")]
    [DataRow(@"<<a\>>>tail", "<<", ">>", null, @"a\>")]
    [DataRow("<a~>b>tail", "<", ">", "~", "a>b")]
    [DataRow("<a~~>tail", "<", ">", "~", "a~")]
    [DataRow("'it''s'tail", "'", "'", "'", "it's")]
    [DataRow("'a''''b'tail", "'", "'", "'", "a''b")]
    [DataRow("\"a\\\"b\"tail", "\"", "\"", null, "a\"b")]
    public void ReadBetween_EscapedContentDecodesOnceAndPreservesCursor(
        string input, string open, string close, string? escape, string expected)
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween(input.AsSpan(), open, close, escaped: true, escapeCharacter: escape);

        Assert.AreEqual(expected, result);
        Assert.AreEqual(input.Length - "tail".Length, interpreter.Position);
        Assert.AreEqual("tail", interpreter.TestReadRest(input.AsSpan()));
    }

    [TestMethod]
    public void ReadBetween_DecodesBeforeTrim()
    {
        var interpreter = new TestTextInterpreter();
        var result = interpreter.TestReadBetween(@"<  a\>b  >tail".AsSpan(), "<", ">", trim: true, escaped: true);

        Assert.AreEqual("a>b", result);
        Assert.AreEqual("tail", interpreter.TestReadRest(@"<  a\>b  >tail".AsSpan()));
    }

    [TestMethod]
    [DataRow(@"<a\>", "<", ">", null)]
    [DataRow("'it''", "'", "'", "'")]
    public void ReadBetween_EscapedClosingDelimiterWithoutTerminatorReportsFieldError(
        string input, string open, string close, string? escape)
    {
        var interpreter = new TestTextInterpreter();
        var exception = Assert.ThrowsExactly<ParseException>(() => interpreter.TestReadBetween(input.AsSpan(),
            open, close, escaped: true, escapeCharacter: escape, fieldName: "Value"));

        Assert.AreEqual(ParseErrorCode.DelimiterNotFound, exception.ErrorCode);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(open.Length, exception.Position);
    }
}
