using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticBinary046SchemaCompositionTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BinarySchema_Comments_ShouldBeRetainedWithSourceSpans()
    {
        const string schemaText = "binary Example {\n" +
                                  "    -- first field\n" +
                                  "    First: byte,\n" +
                                  "    /* second field */\n" +
                                  "    Second: short le,\n" +
                                  "    Third: byte -- trailing field comment\n" +
                                  "}";

        var schema = ParseBinarySchema(schemaText);

        Assert.HasCount(3, schema.Comments);
        Assert.AreEqual("-- first field", schema.Comments[0].Text);
        Assert.AreEqual("/* second field */", schema.Comments[1].Text);
        Assert.AreEqual("-- trailing field comment", schema.Comments[2].Text);

        foreach (var comment in schema.Comments)
        {
            Assert.AreEqual(comment.Text, schemaText.Substring(comment.Span.Start, comment.Span.Length));
            Assert.IsTrue(schema.Span.Contains(comment.Span));
        }

        Assert.AreEqual(new TextSpan(schemaText.IndexOf("First", StringComparison.Ordinal), 5), schema.Fields[0].Span);
        Assert.AreEqual(new TextSpan(schemaText.IndexOf("Second", StringComparison.Ordinal), 6), schema.Fields[1].Span);
    }

    [TestMethod]
    public void BinarySchema_DuplicateGenericParameter_ShouldBeRejectedAtParameterSpan()
    {
        const string schemaText = "binary Pair<T, t> { First: T, Second: t }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ2012_InvalidSchemaDefinition, exception.Code);
        Assert.AreEqual(new TextSpan(schemaText.IndexOf("t>", StringComparison.Ordinal), 1), exception.Span);
    }
}
