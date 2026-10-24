using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class BinaryCompositionParserTests : SchemaParserTestsBase
{
    [TestMethod]
    public void ArrayOfStructuredSubstreams_ShouldKeepArrayOutsideTheSliceTarget()
    {
        var schema = ParseBinarySchema("binary Rec { Items: substream[1] as { Value: byte } exact[2] }");
        var field = (FieldDefinitionNode)schema.Fields[0];
        var array = Assert.IsInstanceOfType<ArrayTypeNode>(field.TypeAnnotation);
        var substream = Assert.IsInstanceOfType<SubstreamTypeNode>(array.ElementType);
        Assert.IsInstanceOfType<InlineSchemaTypeNode>(substream.Target);
    }

    [TestMethod]
    public void GroupedRepeat_ShouldBindArrayElementsAndPreserveFollowingFields()
    {
        var schema = ParseBinarySchema("binary Rec { Count: byte, Copy: (Count + 1), Items: (byte repeat until Items[-1] = 0)[2], Tail: byte }");
        Assert.IsInstanceOfType<ComputedFieldNode>(schema.Fields[1]);
        var array = Assert.IsInstanceOfType<ArrayTypeNode>(((FieldDefinitionNode)schema.Fields[2]).TypeAnnotation);
        Assert.AreEqual("Items", Assert.IsInstanceOfType<RepeatUntilTypeNode>(array.ElementType).FieldName);
        Assert.AreEqual("Tail", schema.Fields[3].Name);
    }

    [TestMethod]
    [DataRow("switch Kind { 1 => Value: byte }[2]")]
    [DataRow("align[8][2]")]
    [DataRow("align[8] repeat until eof")]
    public void InvalidValueContexts_ShouldFailBeforeInterpretation(string type)
    {
        var failure = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema($"binary Rec {{ Kind: byte, Items: {type} }}"));
        Assert.AreEqual(DiagnosticCode.MQ4001_InvalidBinarySchemaField, failure.Code);
    }
}
