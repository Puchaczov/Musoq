using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourcePredicateScopedBindingTests
{
    [TestMethod]
    public void ReusedAlias_ShouldUseStabilityFromItsOwnSourceScope()
    {
        var volatileSource = new SchemaFromNode("#volatile", "items", new ArgsListNode([]), "s", typeof(object), 1);
        var stableSource = new SchemaFromNode("#stable", "items", new ArgsListNode([]), "s", typeof(object), 2);
        ISchemaColumn[] stable = [new SchemaColumn("Score", 0, typeof(int), ColumnStability.Stable)];
        ISchemaColumn[] volatileColumns = [new SchemaColumn("Score", 0, typeof(int), ColumnStability.Volatile)];
        var where = new WhereNode(new EqualityNode(new AccessColumnNode("Score", "s", typeof(int), default), new IntegerNode("0", "i")));
        var result = SourcePredicatePlanner.Plan(
            new Dictionary<SchemaFromNode, WhereNode> { [volatileSource] = where, [stableSource] = where },
            new Dictionary<string, ISchemaColumn[]> { ["s"] = stable },
            sourceColumns: new Dictionary<SchemaFromNode, ISchemaColumn[]> { [volatileSource] = volatileColumns, [stableSource] = stable });
        Assert.AreEqual(0, result.PlansBySourceId[volatileSource.Id].PushedPredicates.Length);
        Assert.AreEqual(1, result.PlansBySourceId[stableSource.Id].PushedPredicates.Length);
    }
}
