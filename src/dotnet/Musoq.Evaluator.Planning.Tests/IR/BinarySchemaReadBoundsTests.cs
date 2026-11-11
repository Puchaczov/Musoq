using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class BinarySchemaReadBoundsTests
{
    [TestMethod]
    public void BoundedCount_WithNestedArray_ShouldUseFieldRangeRatherThanInputLength()
    {
        var registry = new SchemaRegistry();
        registry.Register("Item", new BinarySchemaNode("Item", [Field("Number", PrimitiveTypeName.UShort)]));
        registry.Register("Envelope", new BinarySchemaNode("Envelope",
        [
            Field("Size", PrimitiveTypeName.Byte),
            new FieldDefinitionNode("Items", new ArrayTypeNode(new SchemaReferenceTypeNode("Item"), new IdentifierNode("Size")))
        ]));
        Assert.AreEqual(511, new BinarySchemaReadBounds(registry, CancellationToken.None).MaximumBytes("Envelope"));
    }

    [TestMethod]
    public void ConditionalFieldsAndAlignment_ShouldIncludeMaximumPaddingAndBothOutcomes()
    {
        var registry = new SchemaRegistry();
        registry.Register("Aligned", new BinarySchemaNode("Aligned",
        [
            Field("Flag", PrimitiveTypeName.Byte),
            new FieldDefinitionNode("Padding", new AlignmentNode(32)),
            new FieldDefinitionNode("Value", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian),
                whenCondition: new BooleanNode(true))
        ]));
        Assert.AreEqual(9, new BinarySchemaReadBounds(registry, CancellationToken.None).MaximumBytes("Aligned"));
    }

    [TestMethod]
    public void UnknownOrOverflowingSize_ShouldRetainCompleteInput()
    {
        var registry = new SchemaRegistry();
        registry.Register("Unknown", new BinarySchemaNode("Unknown",
            [new FieldDefinitionNode("Data", new ByteArrayTypeNode(new IdentifierNode("External")))]));
        registry.Register("Large", new BinarySchemaNode("Large",
        [
            new FieldDefinitionNode("Data", new ArrayTypeNode(
                new PrimitiveTypeNode(PrimitiveTypeName.Long, Endianness.LittleEndian), new IntegerNode("2147483647")))
        ]));
        var bounds = new BinarySchemaReadBounds(registry, CancellationToken.None);
        Assert.IsNull(bounds.MaximumBytes("Unknown"));
        Assert.IsNull(bounds.MaximumBytes("Large"));
    }

    [TestMethod]
    public void SwitchWithAnInputLengthDependentBranch_ShouldRetainCompleteInput()
    {
        var registry = new SchemaRegistry();
        var value = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.LittleEndian);
        registry.Register("Remainder", new BinarySchemaNode("Remainder",
            [new FieldDefinitionNode("Bytes", RepeatUntilTypeNode.EndOfInput(value, "Bytes"))]));
        registry.Register("Packet", new BinarySchemaNode("Packet",
        [
            Field("Flag", PrimitiveTypeName.Byte),
            new FieldDefinitionNode("Body", new BinarySwitchTypeNode("Flag",
            [
                new BinarySwitchCaseNode(new IntegerNode("0"), "Scalar", value),
                new BinarySwitchCaseNode(null, "Remainder", new SchemaReferenceTypeNode("Remainder"))
            ]))
        ]));

        Assert.IsNull(new BinarySchemaReadBounds(registry, CancellationToken.None).MaximumBytes("Packet"));
    }

    [TestMethod]
    public void Cancellation_ShouldBeObservedBeforeSchemaAnalysis()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsExactly<System.OperationCanceledException>(() =>
            new BinarySchemaReadBounds(new SchemaRegistry(), cancellation.Token).MaximumBytes("Missing"));
    }

    private static FieldDefinitionNode Field(string name, PrimitiveTypeName type) =>
        new(name, new PrimitiveTypeNode(type, Endianness.LittleEndian));
}
