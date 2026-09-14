using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
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
        Assert.HasCount(10, result.Trace.Entries);
        Assert.AreEqual("SingleUsePipelineFusion", result.Trace.Entries[0].PassName);
        Assert.AreEqual("CteReadOnceFusion", result.Trace.Entries[1].PassName);
        Assert.AreEqual("CteSidecarIndexLowering", result.Trace.Entries[2].PassName);
        Assert.AreEqual("LikeStrategyLowering", result.Trace.Entries[3].PassName);
        Assert.AreEqual("MethodTargetReuse", result.Trace.Entries[4].PassName);
        Assert.AreEqual("LoopInvariantCodeMotion", result.Trace.Entries[5].PassName);
        Assert.AreEqual("FieldExpressionHoisting", result.Trace.Entries[6].PassName);
        Assert.AreEqual("ExpressionCseHoisting", result.Trace.Entries[7].PassName);
        Assert.AreEqual("CapacityHints", result.Trace.Entries[8].PassName);
        Assert.AreEqual("MethodTargetReuse", result.Trace.Entries[9].PassName);
        Assert.IsFalse(result.Trace.Entries.Any(entry => entry.IsChanged));
        AssertTraceEntriesAreMeaningful(result.Trace.Entries);
    }

    [TestMethod]
    public void Optimize_WhenConstantLikePatternIsPresent_ShouldLowerToStringMatch()
    {
        var patternMatch = new ExecutionPatternMatch(
            new ExecutionLiteral("Google0@example.com", typeof(string)),
            new ExecutionLiteral("Google%", typeof(string)),
            PatternKind.Like,
            typeof(bool));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(Var("matched", typeof(bool)), patternMatch)
            ]));

        var result = new ExecutionIrOptimizer().Optimize(plan);
        var optimizedPattern = ExecutionIrAnalysis
            .CollectExpressions<ExecutionStringMatch>(result.OptimizedPlan.Body)
            .Single();

        Assert.AreEqual(ExecutionStringMatchKind.Prefix, optimizedPattern.Kind);
        Assert.AreEqual(ExecutionStringMatchComparison.LikeIgnoreCase, optimizedPattern.Comparison);
        Assert.AreEqual("Google%", optimizedPattern.OriginalPattern);
        Assert.AreEqual("Google", optimizedPattern.Needle);
        StringAssert.Contains(result.Trace.Entries[3].Reason, "direct=1");
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

    [TestMethod]
    public void Optimize_WhenLikeShapesVary_ShouldLowerEveryLikeToAnExplicitStrategy()
    {
        var value = new ExecutionLiteral("value", typeof(string));
        var dynamicPattern = new ExecutionVariable("pattern", typeof(string));
        var patterns = new ExecutionExpression[]
        {
            new ExecutionLiteral("value%", typeof(string)),
            new ExecutionLiteral("v_lue", typeof(string)),
            new ExecutionLiteral("v%lue", typeof(string)),
            new ExecutionLiteral("Ż%", typeof(string)),
            new ExecutionVariableRead(dynamicPattern)
        };
        var body = patterns
            .Select((pattern, index) => (ExecutionNode)new ExecutionLet(
                Var($"matched{index}", typeof(bool)),
                new ExecutionPatternMatch(value, pattern, PatternKind.Like, typeof(bool))))
            .ToArray();

        var result = new ExecutionIrOptimizer().Optimize(new ExecutionPlan("compiled", [], new ExecutionBlock(body)));
        var trace = result.Trace.Entries.Single(entry => entry.PassName == "LikeStrategyLowering");

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionStringMatch>(result.OptimizedPlan.Body));
        Assert.HasCount(3, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedLikeMatch>(result.OptimizedPlan.Body));
        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicLikeMatch>(result.OptimizedPlan.Body));
        Assert.IsFalse(ExecutionIrAnalysis.CollectExpressions<ExecutionPatternMatch>(result.OptimizedPlan.Body)
            .Any(static match => match.Kind == PatternKind.Like));
        StringAssert.Contains(trace.Reason, "direct=1");
        StringAssert.Contains(trace.Reason, "prepared=3");
        StringAssert.Contains(trace.Reason, "dynamic=1");
        StringAssert.Contains(trace.Reason, "serial-cache=1");
    }

    [TestMethod]
    public void Optimize_WhenPreparedPatternsRepeat_ShouldPrepareEachDistinctPatternOnce()
    {
        var value = new ExecutionLiteral("value", typeof(string));
        var patterns = new[] { "v_lue", "v_lue", "Ż%", "Ż%" };
        var body = patterns
            .Select((pattern, index) => (ExecutionNode)new ExecutionLet(
                Var($"matched{index}", typeof(bool)),
                new ExecutionPatternMatch(
                    value,
                    new ExecutionLiteral(pattern, typeof(string)),
                    PatternKind.Like,
                    typeof(bool))))
            .ToArray();

        var result = new ExecutionIrOptimizer().Optimize(new ExecutionPlan("compiled", [], new ExecutionBlock(body)));

        Assert.HasCount(2, ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.OptimizedPlan.Body));
        Assert.HasCount(4, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedLikeMatch>(result.OptimizedPlan.Body));
    }

    [TestMethod]
    public void Optimize_WhenUserVariableCollidesWithLikeLocals_ShouldGenerateDistinctNames()
    {
        var pattern = Var("pattern", typeof(string));
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(Var("__likeCache0", typeof(string)), new ExecutionLiteral("occupied", typeof(string))),
                new ExecutionLet(Var("__likeMatcher0", typeof(string)), new ExecutionLiteral("occupied", typeof(string))),
                new ExecutionLet(
                    Var("dynamicMatch", typeof(bool)),
                    new ExecutionPatternMatch(
                        new ExecutionLiteral("value", typeof(string)),
                        new ExecutionVariableRead(pattern),
                        PatternKind.Like,
                        typeof(bool))),
                new ExecutionLet(
                    Var("preparedMatch", typeof(bool)),
                    new ExecutionPatternMatch(
                        new ExecutionLiteral("value", typeof(string)),
                        new ExecutionLiteral("v_lue", typeof(string)),
                        PatternKind.Like,
                        typeof(bool)))
            ]));

        var result = new ExecutionIrOptimizer().Optimize(plan);
        var names = ExecutionIrAnalysis.CollectDeclaredVariableNames(result.OptimizedPlan.Body).ToArray();

        Assert.Contains("__likeCache1", names);
        Assert.Contains("__likeMatcher1", names);
        Assert.AreEqual(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void Optimize_WhenDynamicLikeRunsInParallelProjection_ShouldUseOneWorkerLocalCacheSlot()
    {
        var source = Var("source", typeof(object));
        var rows = Var("rows", typeof(object));
        var pattern = Var("pattern", typeof(string));
        var table = Var("result", typeof(object));
        var shape = new GeneratedRowShape("ResultRow0", []);
        var match = new ExecutionPatternMatch(
            new ExecutionLiteral("value", typeof(string)),
            new ExecutionVariableRead(pattern),
            PatternKind.Like,
            typeof(bool));
        var append = new ExecutionAppendRow(table, shape, []);
        var parallel = new ExecutionParallelFilterProjectLoop(
            source,
            new ExecutionVariableRead(rows),
            match,
            append,
            new ExecutionBlock([new ExecutionIf(match, new ExecutionBlock([append]))]),
            1,
            4);

        var result = new ExecutionIrOptimizer().Optimize(
            new ExecutionPlan("compiled", [shape], new ExecutionBlock([parallel])));
        var slots = ExecutionIrAnalysis
            .CollectExpressions<ExecutionLikeMatcherCacheSlot>(result.OptimizedPlan.Body)
            .ToArray();

        Assert.HasCount(1, slots);
        Assert.IsTrue(slots[0].WorkerLocal);
        Assert.HasCount(2, ExecutionIrAnalysis
            .CollectExpressions<ExecutionDynamicLikeMatch>(result.OptimizedPlan.Body));
    }

    [TestMethod]
    public void Optimize_WhenPreparedPatternRepeatsAcrossParallelTasks_ShouldPrepareOnceAtExecutionRoot()
    {
        var patternMatch = new ExecutionPatternMatch(
            new ExecutionLiteral("value", typeof(string)),
            new ExecutionLiteral("v_lue", typeof(string)),
            PatternKind.Like,
            typeof(bool));
        var parallel = new ExecutionParallelBlock(
            "like",
            2,
            [
                new ExecutionParallelTask(
                    "first",
                    Var("firstOutput", typeof(object)),
                    new ExecutionBlock([new ExecutionLet(Var("firstMatch", typeof(bool)), patternMatch)])),
                new ExecutionParallelTask(
                    "second",
                    Var("secondOutput", typeof(object)),
                    new ExecutionBlock([new ExecutionLet(Var("secondMatch", typeof(bool)), patternMatch)]))
            ],
            new ExecutionParallelMerge(ExecutionBlock.Empty));

        var result = new ExecutionIrOptimizer().Optimize(
            new ExecutionPlan("compiled", [], new ExecutionBlock([parallel])));

        Assert.IsTrue(result.OptimizedPlan.Body.Nodes[0] is ExecutionLet
        {
            Value: ExecutionPrepareLikeMatcher
        });
        Assert.HasCount(1, ExecutionIrAnalysis
            .CollectExpressions<ExecutionPrepareLikeMatcher>(result.OptimizedPlan.Body));
        Assert.HasCount(2, ExecutionIrAnalysis
            .CollectExpressions<ExecutionPreparedLikeMatch>(result.OptimizedPlan.Body));
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
