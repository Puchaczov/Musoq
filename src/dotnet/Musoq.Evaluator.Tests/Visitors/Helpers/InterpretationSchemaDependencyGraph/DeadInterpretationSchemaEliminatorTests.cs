using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.InterpretationSchemaDependencyGraph;

[TestClass]
public class DeadInterpretationSchemaEliminatorTests
{
    [TestMethod]
    public void Analyze_WhenInterpretUsesSchema_ShouldMarkSchemaAsReachable()
    {
        var registry = new SchemaRegistry();
        registry.Register("A", CreateBinarySchema("A"));
        registry.Register("B", CreateBinarySchema("B"));

        var queryTree = ParseQuery("select Interpret<A>(d.Dummy) from #system.dual() d");

        var graph = DeadInterpretationSchemaEliminator.Analyze(queryTree, registry);

        Assert.IsTrue(graph.Nodes["A"].IsReachable);
        Assert.IsFalse(graph.Nodes["B"].IsReachable);
        Assert.HasCount(1, graph.DeadSchemas);
    }

    [TestMethod]
    public void Eliminate_WhenUnusedSchemasExist_ShouldPruneOnlyUnusedSchemas()
    {
        var registry = new SchemaRegistry();
        registry.Register("A", CreateBinarySchema("A"));
        registry.Register("B", CreateBinarySchema("B"));

        var queryTree = ParseQuery("select Interpret<A>(d.Dummy) from #system.dual() d");

        var result = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);

        Assert.IsTrue(result.WereSchemasEliminated);
        Assert.IsFalse(result.AllSchemasEliminated);
        Assert.AreEqual(1, result.EliminatedCount);
        Assert.AreEqual(1, result.ResultRegistry.Count);
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("A"));
        Assert.IsFalse(result.ResultRegistry.ContainsSchema("B"));
    }

    [TestMethod]
    public void Eliminate_WhenUsedSchemaDependsOnOtherSchema_ShouldKeepDependency()
    {
        var registry = new SchemaRegistry();
        registry.Register("Child", CreateBinarySchema("Child"));

        var parent = new BinarySchemaNode(
            "Parent",
            [new FieldDefinitionNode("Payload", new SchemaReferenceTypeNode("Child"))]);
        registry.Register("Parent", parent);

        var queryTree = ParseQuery("select Interpret<Parent>(d.Dummy) from #system.dual() d");

        var result = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);

        Assert.IsFalse(result.WereSchemasEliminated);
        Assert.AreEqual(2, result.ResultRegistry.Count);
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Parent"));
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Child"));
    }

    [TestMethod]
    public void Analyze_WhenParseInApplyUsesSchema_ShouldMarkSchemaAsReachable()
    {
        var registry = new SchemaRegistry();
        registry.Register("Data", CreateTextSchema("Data"));
        registry.Register("Unused", CreateTextSchema("Unused"));

        var queryTree = ParseQuery("select d.Value from #system.dual() s cross apply Parse<Data>(s.Dummy) d");

        var graph = DeadInterpretationSchemaEliminator.Analyze(queryTree, registry);

        Assert.IsTrue(graph.Nodes["Data"].IsReachable);
        Assert.IsFalse(graph.Nodes["Unused"].IsReachable);
    }

    [TestMethod]
    public void Eliminate_WhenTextSchemaRepeatsAnotherSchema_ShouldKeepRepeatedSchema()
    {
        var registry = new SchemaRegistry();
        registry.Register("Pair", CreateTextSchema("Pair"));

        var configSchema = new TextSchemaNode("Config",
            [new TextFieldDefinitionNode("Entries", TextFieldType.Repeat, "Pair")]);
        registry.Register("Config", configSchema);

        var queryTree = ParseQuery("select c from #system.dual() d cross apply Parse<Config>(d.Dummy) c");

        var result = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);

        Assert.AreEqual(2, result.ResultRegistry.Count);
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Config"));
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Pair"));
    }

    [TestMethod]
    public void Eliminate_WhenBinarySchemaUsesRepeatUntil_ShouldKeepElementSchema()
    {
        var registry = new SchemaRegistry();
        registry.Register("Record", CreateBinarySchema("Record"));

        var streamSchema = new BinarySchemaNode("Stream",
        [
            new FieldDefinitionNode("Records", new RepeatUntilTypeNode(
                new SchemaReferenceTypeNode("Record"),
                new BooleanNode(true),
                "Records"))
        ]);
        registry.Register("Stream", streamSchema);

        var queryTree = ParseQuery("select s from #system.dual() d cross apply Interpret<Stream>(d.Dummy) s");

        var result = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);

        Assert.AreEqual(2, result.ResultRegistry.Count);
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Stream"));
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Record"));
    }

    [TestMethod]
    public void Eliminate_WhenParsedTextSchemaUsesRepeat_ShouldKeepReferencedSchema()
    {
        var queryTree = ParseQuery(@"
            text Pair { Key: until '=', Value: rest };
            text Config { Entries: repeat Pair until end };
            select c from #system.dual() d cross apply Parse<Config>(d.Dummy) c");

        var registry = new SchemaRegistry();
        var definitionVisitor = new SchemaDefinitionVisitor(registry);
        var traverseVisitor = new SchemaDefinitionTestTraverseVisitor(definitionVisitor);
        queryTree.Accept(traverseVisitor);

        var result = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);

        Assert.AreEqual(2, result.ResultRegistry.Count);
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Config"));
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Pair"));
    }

    [TestMethod]
    public void Eliminate_WhenNoInterpretationSchemasAreUsed_ShouldEliminateAllSchemas()
    {
        var registry = new SchemaRegistry();
        registry.Register("A", CreateBinarySchema("A"));
        registry.Register("B", CreateTextSchema("B"));

        var queryTree = ParseQuery("select 1 from #system.dual()");

        var result = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);

        Assert.IsTrue(result.WereSchemasEliminated);
        Assert.IsTrue(result.AllSchemasEliminated);
        Assert.AreEqual(2, result.EliminatedCount);
        Assert.AreEqual(0, result.ResultRegistry.Count);
    }

    private static RootNode ParseQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        return parser.ComposeAll();
    }

    private static BinarySchemaNode CreateBinarySchema(string name)
    {
        return new BinarySchemaNode(
            name,
            [new FieldDefinitionNode("Value", new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))]);
    }

    private static TextSchemaNode CreateTextSchema(string name)
    {
        return new TextSchemaNode(name, [new TextFieldDefinitionNode("Value", TextFieldType.Rest)]);
    }

    private sealed class SchemaDefinitionTestTraverseVisitor(SchemaDefinitionVisitor visitor)
        : RawTraverseVisitor<SchemaDefinitionVisitor>(visitor);
}
