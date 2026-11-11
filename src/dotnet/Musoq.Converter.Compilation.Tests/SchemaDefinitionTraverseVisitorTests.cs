using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class SchemaDefinitionTraverseVisitorTests
{
    [TestMethod]
    public void Walk_WhenRootContainsSchemaStatements_ShouldRegisterEverySchemaDefinition()
    {
        var registry = new SchemaRegistry();
        var traverseVisitor = new SchemaDefinitionTraverseVisitor(new SchemaDefinitionVisitor(registry));
        var root = BuildRootWithSchemas("Header", "LogLine");

        traverseVisitor.Walk(root);

        CollectionAssert.AreEqual(
            new[] { "Header", "LogLine" },
            registry.Schemas.Select(static schema => schema.Name).ToArray());
    }

    [TestMethod]
    public void Walk_WhenBinarySchemaPresent_ShouldRegisterBinarySchemaNode()
    {
        var registry = new SchemaRegistry();
        var traverseVisitor = new SchemaDefinitionTraverseVisitor(new SchemaDefinitionVisitor(registry));
        var root = BuildRootWithSchemas("Header", "LogLine");

        traverseVisitor.Walk(root);

        Assert.IsTrue(registry.TryGetSchema("Header", out var registration) && registration!.Node is BinarySchemaNode);
    }

    [TestMethod]
    public void Walk_WhenTextSchemaPresent_ShouldRegisterTextSchemaNode()
    {
        var registry = new SchemaRegistry();
        var traverseVisitor = new SchemaDefinitionTraverseVisitor(new SchemaDefinitionVisitor(registry));
        var root = BuildRootWithSchemas("Header", "LogLine");

        traverseVisitor.Walk(root);

        Assert.IsTrue(registry.TryGetSchema("LogLine", out var registration) && registration!.Node is TextSchemaNode);
    }

    private static RootNode BuildRootWithSchemas(string binaryName, string textName)
    {
        var statements = new[]
        {
            new StatementNode(new BinarySchemaNode(binaryName, [])),
            new StatementNode(new TextSchemaNode(textName, []))
        };

        return new RootNode(new StatementsArrayNode(statements));
    }
}
