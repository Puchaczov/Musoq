using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticBinary043RepetitionDiscardTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BinaryRepetitionAndConditionalDiscard_ShouldPreserveSchemaShape()
    {
        const string schema =
            "binary Stream {" +
            " Count: byte," +
            " Items: byte repeat until Items[-1] = 0," +
            " Chunks: byte[2] repeat until EOF," +
            " _: byte when Count = 1" +
            "}";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(4, result.Fields);

        var items = AssertField(result, "Items");
        var itemsRepeat = items.TypeAnnotation as RepeatUntilTypeNode;
        Assert.IsNotNull(itemsRepeat);
        Assert.AreEqual(RepeatUntilStopKind.Condition, itemsRepeat.StopKind);
        Assert.AreEqual("Items", itemsRepeat.FieldName);
        Assert.IsInstanceOfType<EqualityNode>(itemsRepeat.Condition);
        Assert.IsInstanceOfType<ArrayIndexNode>(((EqualityNode)itemsRepeat.Condition!).Left);

        var chunks = AssertField(result, "Chunks");
        var chunksRepeat = chunks.TypeAnnotation as RepeatUntilTypeNode;
        Assert.IsNotNull(chunksRepeat);
        Assert.AreEqual(RepeatUntilStopKind.EndOfInput, chunksRepeat.StopKind);
        Assert.IsInstanceOfType<ByteArrayTypeNode>(chunksRepeat.ElementType);

        var discard = AssertField(result, "_");
        Assert.IsNotNull(discard.WhenCondition);
        Assert.IsInstanceOfType<EqualityNode>(discard.WhenCondition);
    }

    [TestMethod]
    public void BinaryRepeatUntil_EofKeywordWithExpressionContinuation_ShouldRemainACondition()
    {
        const string schema = "binary Stream { eof: byte, Items: byte repeat until eof = 0 }";

        var result = ParseBinarySchema(schema);
        var repeat = AssertField(result, "Items").TypeAnnotation as RepeatUntilTypeNode;

        Assert.IsNotNull(repeat);
        Assert.AreEqual(RepeatUntilStopKind.Condition, repeat.StopKind);
        Assert.IsInstanceOfType<EqualityNode>(repeat.Condition);
        Assert.IsInstanceOfType<IdentifierNode>(((EqualityNode)repeat.Condition!).Left);
    }

    [TestMethod]
    public void BinaryRepeatUntil_EofSentinel_ShouldAllowFollowingConditionalModifier()
    {
        const string schema = "binary Stream { Flag: byte, Items: byte repeat until eof when Flag = 1 }";

        var result = ParseBinarySchema(schema);
        var items = AssertField(result, "Items");
        var repeat = items.TypeAnnotation as RepeatUntilTypeNode;

        Assert.IsNotNull(repeat);
        Assert.AreEqual(RepeatUntilStopKind.EndOfInput, repeat.StopKind);
        Assert.IsNotNull(items.WhenCondition);
    }

    [TestMethod]
    public void BinaryRepeatUntil_NegativeByteArrayElementSize_ShouldReportExactMq4001Span()
    {
        const string schema = "binary Stream { Chunks: byte[-1] repeat until eof }";
        var start = schema.IndexOf("-1", StringComparison.Ordinal);

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4001_InvalidBinarySchemaField, exception.Code);
        Assert.AreEqual(new TextSpan(start, 2), exception.Span!.Value);
    }

    private static FieldDefinitionNode AssertField(BinarySchemaNode schema, string name)
    {
        var field = schema.Fields.Single(item => item.Name == name) as FieldDefinitionNode;
        Assert.IsNotNull(field);
        return field;
    }
}
