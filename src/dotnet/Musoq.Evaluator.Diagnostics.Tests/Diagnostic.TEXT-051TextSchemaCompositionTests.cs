using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticText051TextSchemaCompositionTests
{
    [TestMethod]
    public void SchemaDefinitionVisitor_ValidDirectReference_ShouldBeAccepted()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);

        visitor.Visit(new TextSchemaNode("Item", [new TextFieldDefinitionNode("Value", TextFieldType.Rest)]));
        visitor.Visit(new TextSchemaNode(
            "Container",
            [new TextFieldDefinitionNode("Header", TextFieldType.SchemaReference, "Item")]));

        Assert.AreEqual(2, registry.Count);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_UnknownDirectReference_ShouldReportUndefinedSchemaAtField()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var field = (TextFieldDefinitionNode)new TextFieldDefinitionNode(
            "Header",
            TextFieldType.SchemaReference,
            "Missing").WithSpan(new TextSpan(20, 6));

        var schema = new TextSchemaNode("Container", [field]);
        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(schema));

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
        Assert.AreEqual(new TextSpan(20, 6), exception.Span);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_ForwardDirectReference_ShouldFailClosed()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var schema = new TextSchemaNode(
            "Container",
            [new TextFieldDefinitionNode("Header", TextFieldType.SchemaReference, "Item")]);

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(schema));

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
        StringAssert.Contains(exception.Message, "undefined schema");
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_DirectReferenceToBinarySchema_ShouldReportInvalidFieldType()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        visitor.Visit(new BinarySchemaNode("Binary", []));
        var schema = new TextSchemaNode(
            "Container",
            [new TextFieldDefinitionNode("Header", TextFieldType.SchemaReference, "Binary")]);

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(schema));

        Assert.AreEqual(DiagnosticCode.MQ4007_InvalidSchemaFieldType, exception.Code);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_RecursiveDirectReference_ShouldReportCircularSchemaReference()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var schema = new TextSchemaNode(
            "Container",
            [new TextFieldDefinitionNode("Header", TextFieldType.SchemaReference, "Container")]);

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(schema));

        Assert.AreEqual(DiagnosticCode.MQ4004_CircularSchemaReference, exception.Code);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_TextInheritance_ShouldValidateDefinitionOrder()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        visitor.Visit(new TextSchemaNode("Base", [new TextFieldDefinitionNode("Prefix", TextFieldType.Literal, "X")]));

        visitor.Visit(new TextSchemaNode(
            "Derived",
            [new TextFieldDefinitionNode("Suffix", TextFieldType.Rest)],
            "Base"));

        Assert.AreEqual(2, registry.Count);
    }

    [TestMethod]
    public void SchemaDefinitionVisitor_UnknownTextBase_ShouldReportUndefinedSchema()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var schema = new TextSchemaNode("Derived", [], "Missing", new TextSpan(12, 7));

        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() => visitor.Visit(schema));

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
        Assert.AreEqual(new TextSpan(12, 7), exception.Span);
    }

    [TestMethod]
    public void DependencyGraph_DirectTextReference_ShouldKeepReferencedSchema()
    {
        var queryTree = ParseQuery(@"
            text Item { Value: rest };
            text Container { Header: Item };
            select c from #system.dual() d cross apply Parse<Container>(d.Dummy) c");

        var registry = new SchemaRegistry();
        var definitionVisitor = new SchemaDefinitionVisitor(registry);
        queryTree.Accept(new SchemaDefinitionTestTraverseVisitor(definitionVisitor));

        var result = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);

        Assert.AreEqual(2, result.ResultRegistry.Count);
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Container"));
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Item"));
    }

    private static RootNode ParseQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        return parser.ComposeAll();
    }

    private sealed class SchemaDefinitionTestTraverseVisitor(SchemaDefinitionVisitor visitor)
        : RawTraverseVisitor<SchemaDefinitionVisitor>(visitor);
}
