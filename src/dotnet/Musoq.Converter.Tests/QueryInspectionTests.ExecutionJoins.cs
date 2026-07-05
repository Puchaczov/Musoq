using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForInnerJoin_ShouldEmitExecutionHashJoinCode()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy",
            new CompilationOptions());

        AssertExecutionPlanContains("CreateHash [eHash: string -> DualEntity]", result.ExecutionPlanText);
        Assert.Contains("new Dictionary<string, HashJoinBucket<", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("eHash.TryGetValue", result.GeneratedCSharpCode);
        Assert.Contains("private sealed class ResultRow0", result.GeneratedCSharpCode);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(d.Dummy, e.Dummy", result.GeneratedCSharpCode);
        Assert.Contains("JoinStrategy [JoinStrategySelection] Inner -> HashJoin", result.PlanningText);
        Assert.Contains("Hash join selected because at least one equi key pair was found.", result.PlanningText);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderInnerHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("CreateHash [eHash: string -> DualEntity]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [eHash[d.Dummy] -> eHashMatches]", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("eHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderLeftOuterHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("CreateHash [eHash: string -> DualEntity]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [eHash[d.Dummy] -> eHashMatches]", result.ExecutionPlanText);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(d.Dummy: d.Dummy, e.Dummy: NULL)]", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("eHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderRightOuterHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d right outer join #system.dual() e on d.Dummy = e.Dummy");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("CreateHash [dHash: string -> DualEntity]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [dHash[e.Dummy] -> dHashMatches]", result.ExecutionPlanText);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(d.Dummy: NULL, e.Dummy: e.Dummy)]", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("dHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCompositeOuterHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy and d.Dummy = e.Dummy");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("CreateHash [eHash: ValueTuple<string, string> -> DualEntity]", result.ExecutionPlanText);
        AssertExecutionPlanContains("Let [dummy: string = d.Dummy]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [eHash[(dummy, dummy)] -> eHashMatches]", result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.CreateNullableHashJoinKey", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderFilteredOuterHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy where e.Dummy is not null");

        AssertUsesExecutionBackend(result);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        Assert.Contains("If [dummy IS NOT NULL]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertGeneratedCSharpContains("eHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.GetColumnValue", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderResidualOuterHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy and e.Dummy = 'missing'");

        Assert.Contains("PhysicalHashJoin [LeftOuter] [build: e.Dummy] [probe: d.Dummy] [residual: (e.Dummy = 'missing')]", result.PhysicalPlanText);
        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("HashProbe [eHash[d.Dummy] -> eHashMatches] [match: eHashHasMatch]", result.ExecutionPlanText);
        Assert.Contains("If [(dummy = 'missing')]", result.ExecutionPlanText);
        AssertExecutionPlanContains("Assign [eHashHasMatch = TRUE]", result.ExecutionPlanText);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("if (!eHashHasMatch)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    [DataRow("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy")]
    [DataRow("select d.Dummy, e.Dummy from #system.dual() d right outer join #system.dual() e on d.Dummy = e.Dummy")]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderSimpleOuterHashJoin_ShouldRunExecutableQuery(string query)
    {
        var compiled = CompileForExecution(query);

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenResidualOuterHashJoinRejectsOnlyBucketCandidate_ShouldReturnNullExtendedRow()
    {
        var compiled = CompileForExecution("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy and e.Dummy = 'missing'");

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenResidualOuterHashJoinMatchesButWhereRejectsMatchedRow_ShouldNotEmitNullFallback()
    {
        var compiled = CompileForExecution("select d.Dummy, e.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy and e.Dummy = 'single' where e.Dummy is null");

        var table = compiled.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCteBackedInnerHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateCteBackedInnerHashJoinQuery());

        AssertUsesExecutionBackend(result);
        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0", result.ExecutionPlanText);
        AssertExecutionPlanContains("StoreCteIndex [hashSidecar0Dummy -> _cteIndexResults.Slot0 Hash]", result.ExecutionPlanText);
        AssertExecutionPlanContains("LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [rHash[l.Dummy] -> rHashMatches]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(l.Dummy: l.Dummy, r.Dummy: r.Dummy)]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[2]]", StringComparison.Ordinal));
        AssertGeneratedCSharpContains("rHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCteBackedInnerHashJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateCteBackedInnerHashJoinQuery());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCteBackedResidualOuterHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateCteBackedResidualOuterHashJoinQuery());

        Assert.Contains("PhysicalHashJoin [LeftOuter] [build: r.Dummy] [probe: l.Dummy] [residual: (r.Dummy = 'missing')]", result.PhysicalPlanText);
        AssertUsesExecutionBackend(result);
        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0", result.ExecutionPlanText);
        AssertExecutionPlanContains("StoreCteIndex [hashSidecar0Dummy -> _cteIndexResults.Slot0 Hash]", result.ExecutionPlanText);
        AssertExecutionPlanContains("LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [rHash[l.Dummy] -> rHashMatches] [match: rHashHasMatch]", result.ExecutionPlanText);
        Assert.Contains("If [(dummy = 'missing')]", result.ExecutionPlanText);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("if (!rHashHasMatch)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCteBackedResidualOuterHashJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateCteBackedResidualOuterHashJoinQuery());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderResidualOuterHashJoinFeedingHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateResidualOuterHashJoinFeedingHashJoinQuery());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("HashProbe [eHash[d.Dummy] -> eHashMatches] [match: eHashHasMatch]", result.ExecutionPlanText);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        Assert.Contains("StoreTable [statement0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        AssertExecutionPlanContains("CreateHash [deHash: string -> Row; capacity: _cteRowResults.Slot0.Count]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashAdd [deHash[de.d.Dummy] += de]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [deHash[f.Dummy] -> deHashMatches]", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("deHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderResidualOuterHashJoinFeedingHashJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateResidualOuterHashJoinFeedingHashJoinQuery());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("single", table[0][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCteBackedInnerNestedLoopJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateCteBackedInnerNestedLoopJoinQuery());

        AssertUsesExecutionBackend(result);
        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("StoreTable [__parallelCteLevel0Task1Result -> _cteRowResults.Slot1", result.ExecutionPlanText);
        Assert.Contains("ForEach [l in _cteRowResults.Slot0]", result.ExecutionPlanText);
        Assert.Contains("ForEach [r in _cteRowResults.Slot1]", result.ExecutionPlanText);
        Assert.Contains("Let [dummy: string = l.Dummy]", result.ExecutionPlanText);
        Assert.Contains("Let [dummy1: string = r.Dummy]", result.ExecutionPlanText);
        Assert.Contains("If [(dummy <> dummy1)]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(l.Dummy: dummy, r.Dummy: dummy1)]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[2]]", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCteBackedInnerNestedLoopJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateCteBackedInnerNestedLoopJoinQuery());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single!", table[0][0]);
        Assert.AreEqual("single", table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderDescTable_ShouldUseExecutionBackend()
    {
        var result = Inspect("desc #system.dual()");

        AssertUsesExecutionBackend(result);
        Assert.Contains("ReturnDesc [#system.dual() Table]", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.GetSpecificTableDescription", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderDescTable_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("desc #system.dual()");

        var table = compiled.Run();

        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Dummy", table[0][0]);
        Assert.AreEqual(0, table[0][1]);
        Assert.AreEqual(typeof(string).FullName, table[0][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderInnerNestedLoopJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy != e.Dummy");

        AssertUsesExecutionBackend(result);
        Assert.Contains("PhysicalNestedLoopJoin [Inner]", result.PhysicalPlanText);
        Assert.Contains("JoinStrategy [JoinStrategySelection] Inner -> NestedLoop", result.PlanningText);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
        AssertExecutionPlanContains("ForEach [d in dRows]", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [e in eRowsBuffer]", result.ExecutionPlanText);
        Assert.Contains("If [(dummy <> dummy1)]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenSemanticNestedLoopJoinIsRequired_ShouldNotWarn()
    {
        var cross = Inspect("select d.Dummy, e.Dummy from #system.dual() d cross join #system.dual() e");
        var asof = Inspect("select d.Dummy, e.Dummy from #system.dual() d asof join #system.dual() e on d.Dummy >= e.Dummy");

        Assert.Contains("PhysicalNestedLoopJoin [Cross]", cross.PhysicalPlanText);
        Assert.Contains("PhysicalNestedLoopJoin [AsofInner]", asof.PhysicalPlanText);
        Assert.IsFalse(cross.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
        Assert.IsFalse(asof.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForInspection_WhenOptimizedJoinStrategiesAreDisabled_ShouldNotWarnForNestedLoop()
    {
        var result = Inspect(
            "select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy != e.Dummy",
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        Assert.Contains("PhysicalNestedLoopJoin [Inner]", result.PhysicalPlanText);
        Assert.Contains("Hash and sort-merge joins are disabled by compilation options.", result.PlanningText);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForInspection_WhenDynamicHashJoinLoweringUsesNestedLoop_ShouldNotWarn()
    {
        var result = Inspect(
            "select l.Name, r.Name from #dynamic.all() l inner join #dynamic.all() r on l.Team = r.Team",
            CreateDynamicRowsSchemaProvider());

        Assert.Contains("PhysicalNestedLoopJoin [Inner]", result.PhysicalPlanText);
        Assert.Contains("dynamic or expando", result.PlanningText);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderInnerNestedLoopJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy != e.Dummy");

        var table = compiled.Run();

        Assert.AreEqual(0, table.Count);
    }


    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForInnerJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[0][1]);
    }

}
