using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ScopedCollectionSourcePlanTests
{
    [TestMethod]
    public void ReusedPropertyAlias_ShouldRetainDeclarationSchemaAndPhysicalSourceIndex()
    {
        const string query = """
            WITH parents AS (
                SELECT p.Array AS Items FROM #A.Entities() s CROSS APPLY s.Children p
            )
            SELECT p.Value FROM parents q CROSS APPLY q.Items p
            """;

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [new("parent")] }
            ),
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );

        var properties = Flatten(inspection.LogicalPlan).OfType<PropertySourceNode>().ToArray();
        var inner = properties.Single(static property => property.SourceAlias == "s");
        var outer = properties.Single(static property => property.SourceAlias == "q");
        Assert.AreEqual(typeof(BasicEntity[]), inner.ResultType);
        Assert.AreEqual(typeof(int[]), inner.OutputSchema.Columns.Single(static column => column.Name == "Array").Type);
        Assert.IsFalse(inner.OutputSchema.Columns.Any(static column => column.Name == "Value"));
        Assert.AreEqual(23, inner.ColumnIndex, "Keep the provider's physical Children index, not an output slot.");
        Assert.AreEqual(typeof(int[]), outer.ResultType);
        Assert.HasCount(1, outer.OutputSchema.Columns);
        Assert.AreEqual("Value", outer.OutputSchema.Columns[0].Name);
        Assert.AreEqual(typeof(int), outer.OutputSchema.Columns[0].Type);
        Assert.AreEqual(0, outer.ColumnIndex);
        var execution = inspection.ExecutionPlan;
        Assert.IsNotNull(execution);
        var reads = ExecutionIrAnalysis
            .CollectExpressions<ExecutionFieldRead>(execution.Body)
            .Where(static read => read.Alias == "p")
            .ToArray();
        Assert.IsNotEmpty(reads);
        Assert.IsTrue(reads.All(static read => read.AccessStrategy != null));
    }

    private static IEnumerable<LogicalNode> Flatten(LogicalNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        foreach (var descendant in Flatten(child))
            yield return descendant;
    }
}
