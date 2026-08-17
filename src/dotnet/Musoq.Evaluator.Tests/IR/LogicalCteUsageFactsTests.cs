using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization.Logical;
using LogicalCteUsageFacts = Musoq.Evaluator.IR.Optimization.Logical.LogicalCteUsageFacts;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalCteUsageFactsTests
{
    [TestMethod]
    public void CollectCteReferences_WhenNodeAndExpressionReferencesExist_ShouldReturnBoth()
    {
        var schema = CreateSchema();
        var input = new CteRefNode("direct", "d", schema);
        var project = new ProjectNode(
            [new ProjectedField("Value", new CteTableRef("expr"), 0)],
            input);

        var references = LogicalCteUsageFacts.CollectCteReferences(project).ToArray();

        CollectionAssert.AreEquivalent(new[] { "direct", "expr" }, references);
    }

    [TestMethod]
    public void ContainsPlanningSensitiveSource_WhenSourceScanIsNested_ShouldReturnTrue()
    {
        var schema = CreateSchema();
        var scan = new SchemaScanNode("#A", "entities", [], "a", schema, "source:3");
        var filter = new FilterNode(new Literal(true, typeof(bool)), scan);
        var values = new ValuesScanNode("v", [], schema);

        Assert.IsTrue(LogicalCteUsageFacts.ContainsPlanningSensitiveSource(filter));
        Assert.IsFalse(LogicalCteUsageFacts.ContainsPlanningSensitiveSource(values));
    }

    [TestMethod]
    public void CollectSchemaSourceOrdinals_WhenSourceContextIdsArePresent_ShouldParseOrdinals()
    {
        var schema = CreateSchema();
        var scan = new SchemaScanNode("#A", "entities", [], "a", schema, "source:12");
        var filter = new FilterNode(new Literal(true, typeof(bool)), scan);

        var ordinals = LogicalSourceOrdinalFacts.CollectSchemaSourceOrdinals(filter).ToArray();

        CollectionAssert.AreEqual(new[] { 12 }, ordinals);
        Assert.IsTrue(LogicalSourceOrdinalFacts.TryParseSourceContextOrdinal("7", out var bareOrdinal));
        Assert.AreEqual(7, bareOrdinal);
        Assert.IsFalse(LogicalSourceOrdinalFacts.TryParseSourceContextOrdinal("source", out _));
    }

    private static OutputSchema CreateSchema()
    {
        return new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]);
    }
}
