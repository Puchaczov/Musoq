using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticText049TextSchemaDefinitionTests
{
    [TestMethod]
    public void SchemaDefinitionVisitor_ValidRepeatAndSwitchReferences_ShouldBeAccepted()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);

        visitor.Visit(new TextSchemaNode("Item", [new TextFieldDefinitionNode("Value", TextFieldType.Rest)]));
        visitor.Visit(new TextSchemaNode(
            "Container",
            [
                new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Item"),
                new TextFieldDefinitionNode(
                    "Content",
                    [new TextSwitchCaseNode("x", "Item"), new TextSwitchCaseNode(null, "Item")])
            ]));

        Assert.AreEqual(2, registry.Count);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_UnknownRepeatReference_ShouldReportUndefinedSchemaAtField()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var field = (TextFieldDefinitionNode)new TextFieldDefinitionNode(
            "Items",
            TextFieldType.Repeat,
            "Missing").WithSpan(new TextSpan(20, 5));

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() =>
            visitor.Visit(new TextSchemaNode("Container", [field])));

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
        Assert.AreEqual(new TextSpan(20, 5), exception.Span);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_UnknownSwitchBranch_ShouldReportUndefinedSchemaAtField()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var field = (TextFieldDefinitionNode)new TextFieldDefinitionNode(
            "Content",
            [new TextSwitchCaseNode("x", "Missing")]).WithSpan(new TextSpan(12, 7));

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() =>
            visitor.Visit(new TextSchemaNode("Container", [field])));

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
        Assert.AreEqual(new TextSpan(12, 7), exception.Span);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_RepeatReferenceNotYetRegistered_ShouldFailClosed()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var container = new TextSchemaNode(
            "Container",
            [new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Item")]);

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(container));

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
        StringAssert.Contains(exception.Message, "undefined schema");
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_TextReferenceToBinarySchema_ShouldReportInvalidFieldType()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        visitor.Visit(new BinarySchemaNode("Binary", []));

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(new TextSchemaNode(
            "Container",
            [new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Binary")])));

        Assert.AreEqual(DiagnosticCode.MQ4007_InvalidSchemaFieldType, exception.Code);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_RecursiveTextReference_ShouldReportCircularSchemaReference()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(new TextSchemaNode(
            "Container",
            [new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Container")])));

        Assert.AreEqual(DiagnosticCode.MQ4004_CircularSchemaReference, exception.Code);
    }
}
