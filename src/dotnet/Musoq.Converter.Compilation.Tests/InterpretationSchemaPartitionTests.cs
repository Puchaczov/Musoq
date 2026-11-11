using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class InterpretationSchemaPartitionTests
{
    [TestMethod]
    public void Create_WhenQueryHasNoSchemas_ReturnsOriginalTreeWithoutSchemaWork()
    {
        var queryTree = Parse("select 1 from #system.dual()");

        var partition = InterpretationSchemaPartition.Create(queryTree);

        Assert.IsFalse(partition.HasDefinitions);
        Assert.AreEqual(0, partition.Registry.Count);
        Assert.AreSame(queryTree, partition.UsageTree);
        Assert.AreSame(queryTree, partition.QueryWithoutDefinitions);
    }

    [TestMethod]
    public void Create_WhenDefinitionsAreMixedWithExecutableStatements_PartitionsOnceAndPreservesOrder()
    {
        var queryTree = Parse(@"
            binary Header { Value: byte };
            select 1 from #system.dual();
            text Trailer { Value: rest };
            select 2 from #system.dual();");

        var partition = InterpretationSchemaPartition.Create(queryTree);

        Assert.IsTrue(partition.HasDefinitions);
        CollectionAssert.AreEqual(
            new[] { "Header", "Trailer" },
            partition.Registry.Schemas.Select(static schema => schema.Name).ToArray());

        var executableStatements = ((StatementsArrayNode)partition.UsageTree.Expression).Statements;
        Assert.HasCount(2, executableStatements);
        Assert.IsTrue(executableStatements.All(static statement =>
            statement.Node is not BinarySchemaNode and not TextSchemaNode));
        Assert.AreSame(partition.UsageTree, partition.QueryWithoutDefinitions);
    }

    [TestMethod]
    public void Create_WhenAllDefinitionsArePresent_UsesEmptyUsageTreeButRetainsOriginalQueryTree()
    {
        var queryTree = Parse(@"
            binary Header { Value: byte };
            text Trailer { Value: rest };");

        var partition = InterpretationSchemaPartition.Create(queryTree);

        var usageStatements = ((StatementsArrayNode)partition.UsageTree.Expression).Statements;
        Assert.HasCount(0, usageStatements);
        Assert.AreSame(queryTree, partition.QueryWithoutDefinitions);
        Assert.AreEqual(queryTree.Span, partition.QueryWithoutDefinitions.Span);
        Assert.AreEqual(queryTree.FullSpan, partition.QueryWithoutDefinitions.FullSpan);
    }

    [TestMethod]
    public void Create_WhenSchemaReferencesAreValid_RegistersThroughTheExistingValidationPath()
    {
        var queryTree = Parse(@"
            binary Child { Value: byte };
            binary Parent { Payload: Child };
            select Interpret<Parent>(d.Dummy) from #system.dual() d;");

        var partition = InterpretationSchemaPartition.Create(queryTree);
        var result = DeadInterpretationSchemaEliminator.Eliminate(partition.UsageTree, partition.Registry);

        Assert.AreEqual(2, result.ResultRegistry.Count);
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Child"));
        Assert.IsTrue(result.ResultRegistry.ContainsSchema("Parent"));
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        return new global::Musoq.Parser.Parser(lexer).ComposeAll();
    }
}
