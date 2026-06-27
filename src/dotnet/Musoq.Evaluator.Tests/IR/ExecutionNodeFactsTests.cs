using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionNodeFactsTests
{
    [TestMethod]
    public void GetChildBlocks_WhenLoopAndProbeNodes_ShouldReturnDirectBlocks()
    {
        var loopBody = new ExecutionBlock([new ExecutionBreak()]);
        var probeBody = new ExecutionBlock([new ExecutionContinue()]);
        var noMatchBody = new ExecutionBlock([new ExecutionBreak()]);
        var loop = new ExecutionForEach(Var("item"), new ExecutionStoredTableRows(0), loopBody);
        var probe = new ExecutionHashProbe(
            Var("hash"),
            Var("matches"),
            Read(Var("key", typeof(int))),
            typeof(int),
            typeof(object),
            probeBody,
            noMatchBody);

        var loopBlocks = ExecutionNodeFacts.GetChildBlocks(loop).ToArray();
        var probeBlocks = ExecutionNodeFacts.GetChildBlocks(probe).ToArray();

        Assert.HasCount(1, loopBlocks);
        Assert.AreSame(loopBody, loopBlocks[0]);
        CollectionAssert.AreEqual(new[] { probeBody, noMatchBody }, probeBlocks);
    }

    [TestMethod]
    public void GetLocalExpressions_WhenWindowAndAggregateNodes_ShouldReturnNodeExpressions()
    {
        var partition = Read(Var("partition", typeof(int)));
        var order = Read(Var("order", typeof(int)));
        var value = Read(Var("value", typeof(int)));
        var aggregateValue = Read(Var("aggregateValue", typeof(int)));
        var window = new ExecutionComputePluginWindow(
            Var("buffer"),
            Var("item"),
            ExecutionRowAccessMode.Direct,
            partition,
            [new ExecutionWindowOrderKey(order, Descending: true)],
            value,
            [new ExecutionLiteral(10, typeof(int))],
            [true],
            null,
            typeof(string).GetMethod(nameof(string.ToString), Type.EmptyTypes)!,
            "Plugin",
            Var("results"));
        var aggregate = new ExecutionAggregateCapturedValueSet(
            Var("group"),
            "Value",
            aggregateValue,
            typeof(int),
            new AggregateCapturedField("Value", "Value", typeof(int)));

        var windowExpressions = ExecutionNodeFacts.GetLocalExpressions(window).ToArray();
        var aggregateExpressions = ExecutionNodeFacts.GetLocalExpressions(aggregate).ToArray();

        Assert.IsTrue(windowExpressions.Contains(partition));
        Assert.IsTrue(windowExpressions.Contains(order));
        Assert.IsTrue(windowExpressions.Contains(value));
        Assert.IsTrue(aggregateExpressions.Contains(aggregateValue));
    }

    [TestMethod]
    public void GetDeclaredVariables_WhenWindowAndAggregateNodes_ShouldReturnDeclaredVariables()
    {
        var shape = new AggregateGroupShape("Group0", [], [], []);
        var plan = new AggregateGroupPlan(shape, [new AggregateGroupLevelPlan(0, shape)]);
        var window = new ExecutionComputeRankingWindow(
            Var("buffer"),
            Var("item"),
            ExecutionRowAccessMode.Direct,
            null,
            [],
            ExecutionRankingWindowFunction.RowNumber,
            Var("results"),
            new ExecutionWindowKeyArray(Var("partitionKeys"), ShouldExtract: true),
            new ExecutionWindowKeyArray(Var("orderKeys"), ShouldExtract: true),
            new ExecutionWindowPartitionSet(Var("partitions"), ShouldCreate: true),
            new ExecutionWindowPartitionSet(Var("sortedPartitions"), ShouldCreate: true));
        var aggregateContext = new ExecutionCreateSingleKeyAggregateContext(
            Var("rootGroup"),
            Var("groups"),
            Var("groupsToFinalize"),
            Var("nullGroup"),
            typeof(int),
            plan);

        CollectionAssert.AreEquivalent(
            new[] { "results", "partitionKeys", "orderKeys", "partitions", "sortedPartitions" },
            Names(ExecutionNodeFacts.GetDeclaredVariables(window)));
        CollectionAssert.AreEquivalent(
            new[] { "rootGroup", "groups", "groupsToFinalize", "nullGroup" },
            Names(ExecutionNodeFacts.GetDeclaredVariables(aggregateContext)));
    }

    [TestMethod]
    public void GetDirectVariableReferences_WhenPostOperationAndCapacityHints_ShouldReturnReferencedVariables()
    {
        var source = Var("source");
        var target = Var("target");
        var capacity = Var("capacity");
        var topOffset = new ExecutionTopOffsetTable(
            source,
            target,
            [],
            1,
            2,
            [],
            ExecutionTopOffsetStrategy.OrderedSlice,
            new ExecutionSkipTakeCapacityHint(capacity, 1, 2));
        var rowsHint = new ExecutionRowsCapacityHintCandidate(
            target,
            new ExecutionRowStream(capacity, ExecutionRowStreamKind.Rows));

        CollectionAssert.AreEquivalent(
            new[] { "source", "target", "capacity" },
            Names(ExecutionNodeFacts.GetDirectVariableReferences(topOffset)));
        CollectionAssert.AreEquivalent(
            new[] { "capacity" },
            Names(ExecutionNodeFacts.GetCapacityHintVariables(rowsHint)));
    }

    [TestMethod]
    public void TryGetTablePostOperation_WhenNodeIsTablePostOperation_ShouldExposeSharedMetadata()
    {
        var source = Var("source");
        var target = Var("target");
        var capacity = new ExecutionConstantCapacityHint(10);
        var node = new ExecutionSkipTable(
            source,
            target,
            3,
            capacity,
            ExecutionAppendMode.Unchecked,
            new ExecutionColumnMetadata("items", [], ExecutionColumnMetadataKind.TableColumns));

        var resolved = ExecutionNodeFacts.TryGetTablePostOperation(node, out var metadata);

        Assert.IsTrue(resolved);
        Assert.IsNotNull(metadata);
        Assert.AreSame(source, metadata.Source);
        Assert.AreSame(target, metadata.Target);
        Assert.AreSame(capacity, metadata.CapacityHint);
        Assert.AreEqual(ExecutionAppendMode.Unchecked, metadata.AppendMode);
        Assert.IsNotNull(metadata.ColumnMetadata);
    }

    [TestMethod]
    public void TryGetWindowComputation_WhenNodeIsWindowComputation_ShouldExposeSharedMetadata()
    {
        var buffer = Var("buffer");
        var item = Var("item");
        var results = Var("results", typeof(long[]));
        var partition = Read(Var("partition", typeof(int)));
        var orderKey = new ExecutionWindowOrderKey(Read(Var("order", typeof(int))), Descending: false);
        var node = new ExecutionComputeRankingWindow(
            buffer,
            item,
            ExecutionRowAccessMode.Direct,
            partition,
            [orderKey],
            ExecutionRankingWindowFunction.Rank,
            results);

        var resolved = ExecutionNodeFacts.TryGetWindowComputation(node, out var metadata);

        Assert.IsTrue(resolved);
        Assert.IsNotNull(metadata);
        Assert.AreSame(buffer, metadata.Buffer);
        Assert.AreSame(item, metadata.Item);
        Assert.AreSame(results, metadata.Results);
        Assert.AreSame(partition, metadata.PartitionKey);
        Assert.AreSame(orderKey, metadata.OrderKeys[0]);
    }

    private static ExecutionVariable Var(string name, Type? type = null)
    {
        return new ExecutionVariable(name, type ?? typeof(object));
    }

    private static ExecutionVariableRead Read(ExecutionVariable variable)
    {
        return new ExecutionVariableRead(variable);
    }

    private static string[] Names(IEnumerable<ExecutionVariable> variables)
    {
        return variables.Select(static variable => variable.Name).ToArray();
    }
}
