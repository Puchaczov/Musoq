using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CapacityHintPassTests
{
    [TestMethod]
    public void Optimize_WhenNoCandidatesArePresent_ShouldLeaveTableWithoutCapacityHint()
    {
        var plan = CreatePlan(CreateTableAppendBlock(new ExecutionStoredTableRows(3)));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
        Assert.Contains("no capacity hint candidates were present", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenRowsCandidateOnTableUsesStoredRows_ShouldLowerToStoredTableCountHint()
    {
        var table = Var("result");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionCreateTable(
                table,
                CreateRowShape(),
                new ExecutionRowsCapacityHintCandidate(table, new ExecutionStoredTableRows(3)))
        ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var createTable = (ExecutionCreateTable)result.Plan.Body.Nodes[0];
        var hint = (ExecutionStoredTableCountCapacityHint)createTable.CapacityHint!;

        Assert.AreEqual(3, hint.TableIndex);
        Assert.Contains("Consumed 1 capacity hint candidate(s)", result.Reason);
        Assert.Contains("[Rows=1]", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenRowsCandidateUsesStoredRows_ShouldLowerToStoredTableCountHint()
    {
        var hash = Var("ordersHash");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionCreateHash(
                hash,
                typeof(int),
                typeof(object),
                new ExecutionRowsCapacityHintCandidate(hash, new ExecutionStoredTableRows(4)))
        ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var createHash = (ExecutionCreateHash)result.Plan.Body.Nodes[0];
        var hint = (ExecutionStoredTableCountCapacityHint)createHash.CapacityHint!;

        Assert.AreEqual(4, hint.TableIndex);
    }

    [TestMethod]
    public void Optimize_WhenRowsCandidateUsesChunkedSourceRows_ShouldLeaveHintUnchanged()
    {
        var target = Var("ordersHash");
        var rows = Var("ordersRows");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionCreateHash(
                target,
                typeof(int),
                typeof(object),
                new ExecutionRowsCapacityHintCandidate(target, new ExecutionRowStream(rows, ExecutionRowStreamKind.Chunks)))
        ]));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        var createHash = (ExecutionCreateHash)result.Plan.Body.Nodes[0];
        Assert.IsInstanceOfType(createHash.CapacityHint, typeof(ExecutionRowsCapacityHintCandidate));
    }

    [TestMethod]
    public void Optimize_WhenConstantCandidateIsPresent_ShouldLowerToConstantHint()
    {
        var table = Var("result");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionCreateTable(
                table,
                CreateRowShape(),
                ExecutionCapacityHintCandidates.CreateConstantCandidate(table, 16))
        ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var createTable = (ExecutionCreateTable)result.Plan.Body.Nodes[0];
        var hint = (ExecutionConstantCapacityHint)createTable.CapacityHint!;

        Assert.AreEqual(16, hint.Capacity);
    }

    [TestMethod]
    public void Optimize_WhenCollectionCountCandidateIsPresent_ShouldLowerToCollectionCountHint()
    {
        var table = Var("result");
        var groups = Var("groupsToFinalize");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionEnsureTableCapacity(
                table,
                ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(table, groups))
        ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var ensureCapacity = (ExecutionEnsureTableCapacity)result.Plan.Body.Nodes[0];
        var hint = (ExecutionCollectionCountCapacityHint)ensureCapacity.CapacityHint;

        Assert.AreEqual(groups, hint.Collection);
    }

    [TestMethod]
    public void Optimize_WhenStrategyCandidatesArePresent_ShouldLowerToStrategyHints()
    {
        var source = Var("source");
        var skipped = Var("skipped");
        var taken = Var("taken");
        var sliced = Var("sliced");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionSkipTable(
                source,
                skipped,
                1,
                ExecutionCapacityHintCandidates.CreateSkipCandidate(skipped, source, 1)),
            new ExecutionTakeTable(
                skipped,
                taken,
                2,
                ExecutionCapacityHintCandidates.CreateTakeCandidate(taken, skipped, 2)),
            new ExecutionSliceTable(
                taken,
                sliced,
                3,
                4,
                ExecutionCapacityHintCandidates.CreateSkipTakeCandidate(sliced, taken, 3, 4))
        ]));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var skip = (ExecutionSkipTable)result.Plan.Body.Nodes[0];
        var take = (ExecutionTakeTable)result.Plan.Body.Nodes[1];
        var slice = (ExecutionSliceTable)result.Plan.Body.Nodes[2];
        var skipHint = (ExecutionSkipCapacityHint)skip.CapacityHint!;
        var takeHint = (ExecutionTakeCapacityHint)take.CapacityHint!;
        var sliceHint = (ExecutionSkipTakeCapacityHint)slice.CapacityHint!;

        Assert.AreEqual(source, skipHint.Collection);
        Assert.AreEqual(1, skipHint.Count);
        Assert.AreEqual(skipped, takeHint.Collection);
        Assert.AreEqual(2, takeHint.Count);
        Assert.AreEqual(taken, sliceHint.Collection);
        Assert.AreEqual(3, sliceHint.SkipCount);
        Assert.AreEqual(4, sliceHint.TakeCount);
        Assert.Contains("Consumed 3 capacity hint candidate(s)", result.Reason);
        Assert.Contains("[Skip=1, SkipTake=1, Take=1]", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenFinalPostOperationHintsArePresent_ShouldCountFinalizedHints()
    {
        var source = Var("source");
        var sorted = Var("sorted");
        var skipped = Var("skipped");
        var taken = Var("taken");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionSortTable(
                source,
                sorted,
                [],
                [],
                new ExecutionCollectionCountCapacityHint(source)),
            new ExecutionSkipTable(
                sorted,
                skipped,
                1,
                new ExecutionSkipCapacityHint(sorted, 1)),
            new ExecutionTakeTable(
                skipped,
                taken,
                2,
                new ExecutionTakeCapacityHint(skipped, 2))
        ]));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
        Assert.Contains("Observed 3 finalized capacity hint(s)", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenUnsupportedCandidateIsPresent_ShouldReportSkippedCandidateKind()
    {
        var hash = Var("hash");
        var plan = CreatePlan(new ExecutionBlock(
        [
            new ExecutionCreateHash(
                hash,
                typeof(int),
                typeof(object),
                new ExecutionRowsCapacityHintCandidate(hash, new ExecutionLiteral(0, typeof(int))))
        ]));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
        Assert.Contains("Consumed 0 capacity hint candidate(s)", result.Reason);
        Assert.Contains("skipped 1 unsupported candidate(s) [Rows=1]", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenCreatedTableHasNoAppendLoop_ShouldLeavePlanUnchanged()
    {
        var plan = CreatePlan(new ExecutionBlock([new ExecutionCreateTable(Var("result"), CreateRowShape())]));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    private static OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan)
    {
        return new CapacityHintPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));
    }

    private static ExecutionPlan CreatePlan(ExecutionBlock block)
    {
        return new ExecutionPlan("compiled", [], block);
    }

    private static ExecutionBlock CreateTableAppendBlock(ExecutionExpression rows)
    {
        var table = Var("result");
        var item = Var("item", typeof(object));
        var append = new ExecutionAppendRow(
            table,
            CreateRowShape(),
            [new ExecutionRowValue("Value", new ExecutionLiteral(1, typeof(int)))]);

        return new ExecutionBlock(
        [
            new ExecutionCreateTable(table, CreateRowShape()),
            new ExecutionForEach(item, rows, new ExecutionBlock([append]))
        ]);
    }

    private static ExecutionVariable Var(string name, Type? type = null)
    {
        return new ExecutionVariable(name, type ?? typeof(object));
    }

    private static GeneratedRowShape CreateRowShape()
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
    }
}
