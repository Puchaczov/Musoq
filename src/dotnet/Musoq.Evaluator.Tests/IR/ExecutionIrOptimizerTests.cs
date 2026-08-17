using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionIrOptimizerTests
{
    [TestMethod]
    public void Optimize_WhenDefaultLocalPassesRun_ShouldReturnInitialPlanAsOptimizedPlan()
    {
        var initial = new ExecutionPlan("compiled", [], ExecutionBlock.Empty);

        var result = new ExecutionIrOptimizer().Optimize(initial);

        Assert.AreSame(initial, result.InitialPlan);
        Assert.AreSame(initial, result.OptimizedPlan);
        Assert.HasCount(8, result.Trace.Entries);
        Assert.AreEqual("SingleUsePipelineFusion", result.Trace.Entries[0].PassName);
        Assert.AreEqual("CteReadOnceFusion", result.Trace.Entries[1].PassName);
        Assert.AreEqual("CteSidecarIndexLowering", result.Trace.Entries[2].PassName);
        Assert.AreEqual("MethodTargetReuse", result.Trace.Entries[3].PassName);
        Assert.AreEqual("FieldExpressionHoisting", result.Trace.Entries[4].PassName);
        Assert.AreEqual("ExpressionCseHoisting", result.Trace.Entries[5].PassName);
        Assert.AreEqual("CapacityHints", result.Trace.Entries[6].PassName);
        Assert.AreEqual("MethodTargetReuse", result.Trace.Entries[7].PassName);
        Assert.IsFalse(result.Trace.Entries.Any(entry => entry.IsChanged));
        AssertTraceEntriesAreMeaningful(result.Trace.Entries);
    }

    [TestMethod]
    public void Optimize_WhenCandidateOnlyIrIsPresent_ShouldConsumeCandidatesBeforeRenderer()
    {
        var rowShape = new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Value", "Value", 0, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
        var resultTable = Var("result", typeof(object));
        var index = Var("cteIndex", typeof(object));
        var target = Var("__library", typeof(LibraryBase));
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var methodCall = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(object))],
            null,
            typeof(string),
            null,
            target);
        var plan = new ExecutionPlan(
            "compiled",
            [rowShape],
            new ExecutionBlock(
            [
                new ExecutionSingleUsePipelineFusionCandidate(1, new ExecutionBlock([Let("singleUse", 1)])),
                new ExecutionCteReadOnceFusionCandidate(2, new ExecutionBlock([Let("readOnce", 2)])),
                new ExecutionCteSidecarIndexBuildCandidate([]),
                new ExecutionCteSidecarAppendRewriteCandidate(
                    new ExecutionAppendRow(
                        resultTable,
                        rowShape,
                        [new ExecutionRowValue("Value", new ExecutionLiteral(4, typeof(int)))]),
                    []),
                new ExecutionCteFusedProducerCandidate([], new ExecutionBlock([Let("fusedProducer", 5)])),
                new ExecutionCteIndexOnlyStorageCandidate("unused", "UnusedRow", false),
                new ExecutionCteSidecarIndexStoreCandidate(index, 0, ExecutionCteSidecarIndexKind.Hash, typeof(int)),
                new ExecutionCteSidecarIndexLoadCandidate(index, 0, ExecutionCteSidecarIndexKind.Hash, typeof(int)),
                new ExecutionCreateTable(
                    resultTable,
                    rowShape,
                    ExecutionCapacityHintCandidates.CreateConstantCandidate(resultTable, 8)),
                new ExecutionHoistCandidateLet(
                    Var("hoistedValue", typeof(int)),
                    new ExecutionLiteral(9, typeof(int)),
                    ExecutionHoistKind.Expression,
                    ExecutionHoistScope.Block,
                    "literal:9"),
                new ExecutionMethodTargetDeclarationCandidate(target),
                new ExecutionLet(
                    Var("typeName", typeof(string)),
                    new ExecutionMethodTargetReuseCandidate(methodCall))
            ]));

        var result = new ExecutionIrOptimizer().Optimize(plan);

        Assert.IsTrue(result.Trace.Entries.Any(static entry => entry.IsChanged));
        Assert.IsFalse(ContainsCandidateNode(result.OptimizedPlan.Body));
        Assert.IsFalse(ContainsMethodTargetCandidateExpression(result.OptimizedPlan.Body));
        Assert.IsFalse(ContainsCapacityHintCandidate(result.OptimizedPlan.Body));
    }

    private static void AssertTraceEntriesAreMeaningful(
        IReadOnlyList<OptimizationTraceEntry> entries)
    {
        Assert.IsTrue(entries.All(static entry => entry.Stage == OptimizationStage.ExecutionIrOptimization));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.PassName)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Outcome)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Reason)));
        Assert.IsTrue(entries.All(static entry =>
            string.Equals(entry.Outcome, entry.IsChanged ? "Changed" : "NoChange", StringComparison.Ordinal)));
    }

    private static bool ContainsCandidateNode(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.FlattenNodes(block).Any(static node => node is
            ExecutionSingleUsePipelineFusionCandidate or
            ExecutionCteReadOnceFusionCandidate or
            ExecutionCteSidecarIndexStoreCandidate or
            ExecutionCteSidecarIndexLoadCandidate or
            ExecutionCteSidecarIndexBuildCandidate or
            ExecutionCteSidecarAppendRewriteCandidate or
            ExecutionCteFusedProducerCandidate or
            ExecutionCteIndexOnlyStorageCandidate or
            ExecutionHoistCandidateLet or
            ExecutionMethodTargetDeclarationCandidate);
    }

    private static bool ContainsMethodTargetCandidateExpression(ExecutionBlock block)
    {
        return ExecutionIrAnalysis
            .CollectExpressions<ExecutionMethodTargetReuseCandidate>(block)
            .Any();
    }

    private static bool ContainsCapacityHintCandidate(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.FlattenNodes(block).Any(static node => node switch
        {
            ExecutionCreateTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionCreateRecordList { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionEnsureTableCapacity { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionCreateHash { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionCreateKeySet { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionSortTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionTopNTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionTopOffsetTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionSkipTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionTakeTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionSliceTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionProjectTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionMaterializeRecordListToTable { CapacityHint: { } hint } => ExecutionCapacityHintCandidates.IsCandidate(hint),
            _ => false
        });
    }

    private static ExecutionLet Let(string name, int value)
    {
        return new ExecutionLet(
            Var(name, typeof(int)),
            new ExecutionLiteral(value, typeof(int)));
    }

    private static ExecutionVariable Var(string name, Type type)
    {
        return new ExecutionVariable(name, type);
    }
}
