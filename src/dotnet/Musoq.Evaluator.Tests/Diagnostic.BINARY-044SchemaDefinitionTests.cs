using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary044SchemaDefinitionTests
{
    [TestMethod]
    public void SchemaDefinitionVisitor_SwitchSelectorMayUseInheritedField()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var parent = new BinarySchemaNode(
            "Base",
            [new FieldDefinitionNode("Type", new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))]);
        visitor.Visit(parent);

        var switchType = new BinarySwitchTypeNode(
            "Type",
            [new BinarySwitchCaseNode(
                new IntegerNode("1", "i"),
                "Code",
                new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))],
            new TextSpan(20, 4));

        visitor.Visit(new BinarySchemaNode(
            "Derived",
            [new FieldDefinitionNode("Payload", switchType)],
            "Base"));

        Assert.IsTrue(registry.ContainsSchema("Derived"));
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_UnknownInheritedSelector_ShouldReportMq4011AtSelector()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        visitor.Visit(new BinarySchemaNode(
            "Base",
            [new FieldDefinitionNode("Type", new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))]));

        var switchType = new BinarySwitchTypeNode(
            "Missing",
            [new BinarySwitchCaseNode(
                new IntegerNode("1", "i"),
                "Code",
                new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))],
            new TextSpan(30, 7));

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(new BinarySchemaNode(
            "Derived",
            [new FieldDefinitionNode("Payload", switchType)],
            "Base")));

        Assert.AreEqual(DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField, exception.Code);
        Assert.AreEqual(new TextSpan(30, 7), exception.Span!.Value);

        var diagnostic = exception.ToDiagnostic();
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind);
        Assert.AreEqual(exception.Span.Value, diagnostic.Span);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_UnsupportedSwitchBranch_ShouldReportMq4013()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var type = new FieldDefinitionNode(
            "Type",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable));
        var schema = new BinarySchemaNode(
            "Packet",
            [
                type,
                new FieldDefinitionNode(
                    "Payload",
                    new BinarySwitchTypeNode(
                        "Type",
                        [new BinarySwitchCaseNode(
                            new IntegerNode("1", "i"),
                            "Flags",
                            new BitsTypeNode(4))]))
            ]);

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(schema));

        Assert.AreEqual(DiagnosticCode.MQ4013_InvalidSwitchCaseLabel, exception.Code);

        var diagnostic = exception.ToDiagnostic();
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind);
        Assert.IsTrue(diagnostic.Span.Length >= 0);
    }
}
