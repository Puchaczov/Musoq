using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenCteSidecarIndexesAreDisabled_ShouldKeepExistingCteHashJoinShape()
    {
        var result = Inspect(CreateCteBackedInnerHashJoinQuery(), CreateCteSidecarDisabledOptions());

        Assert.IsFalse(result.PlanningText.Contains("CteSidecarIndexStrategy", StringComparison.Ordinal));
        AssertExecutionPlanDoesNotContain("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanContains("CreateHash [rHash: string -> Row; capacity: _cteRowResults.Slot1.Count]", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashAdd [rHash[r.Dummy] += r]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSimpleCteIsHashBuildSide_ShouldUseHashSidecar()
    {
        var result = Inspect(CreateCteBackedInnerHashJoinQuery(), CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> Hash", result.PlanningText);
        AssertExecutionPlanContains("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanContains("LoadCteIndex [rHash <- _cteIndexResults.Slot", result.ExecutionPlanText);
        AssertExecutionPlanContains("HashProbe [rHash[l.Dummy] -> rHashMatches]", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("CreateHash [rHash:", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("HashAdd [rHash", result.ExecutionPlanText);
        AssertGeneratedCSharpUsesTypedCteIndexResults(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenSameCteKeyIsUsedByMultipleHashBuildConsumers_ShouldReuseOneHashSidecar()
    {
        const string query = @"
with indexed as (
    select d.Dummy as Dummy
    from #system.dual() d
)
select l.Dummy, r.Dummy, s.Dummy
from indexed l
inner join indexed r on l.Dummy = r.Dummy
inner join indexed s on l.Dummy = s.Dummy";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("slot 0 allocated", result.PlanningText);
        Assert.Contains("slot 0 reused", result.PlanningText);
        Assert.AreEqual(1, CountOccurrences(result.ExecutionPlanText, "StoreCteIndex ["));
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "LoadCteIndex ["));
        AssertExecutionPlanDoesNotContain("CreateHash [rHash:", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("CreateHash [sHash:", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSameCteUsesDifferentHashKeys_ShouldBuildDistinctSidecars()
    {
        const string query = @"
with indexed as (
    select d.Dummy as FirstKey, d.Dummy as SecondKey
    from #system.dual() d
)
select d.Dummy, r.FirstKey, s.SecondKey
from #system.dual() d
inner join indexed r on d.Dummy = r.FirstKey
inner join indexed s on d.Dummy = s.SecondKey";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("slot 0 allocated", result.PlanningText);
        Assert.Contains("slot 1 allocated", result.PlanningText);
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "StoreCteIndex ["));
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "LoadCteIndex ["));
        AssertExecutionPlanContains("_cteIndexResults.Slot0 Hash: string]", result.ExecutionPlanText);
        AssertExecutionPlanContains("_cteIndexResults.Slot1 Hash: string]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSameCteHasHashAndKeySetConsumers_ShouldBuildDistinctKinds()
    {
        const string query = @"
with indexed as (
    select d.Dummy as Dummy
    from #system.dual() d
)
select d.Dummy, r.Dummy
from #system.dual() d
inner join indexed r on d.Dummy = r.Dummy
semi join indexed s on d.Dummy = s.Dummy";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("-> Hash", result.PlanningText);
        Assert.Contains("-> KeySet", result.PlanningText);
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "StoreCteIndex ["));
        AssertExecutionPlanContains("LoadCteIndex [join_1_d_rTableRHash <- _cteIndexResults.Slot", result.ExecutionPlanText);
        AssertExecutionPlanContains("LoadCteIndex [sKeys <- _cteIndexResults.Slot", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("HashAdd [join_1_d_rTableRHash", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("KeySetAdd [sKeys", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenKeySetSidecarOnlyDependsOnProbeAlias_ShouldScheduleItBeforeHashFanout()
    {
        const string query = @"
with raw as (
    select r.Dummy as Dummy
    from #system.dual() r
),
firstCte as (
    select Dummy
    from raw
),
secondCte as (
    select Dummy
    from raw
),
eligible as (
    select Dummy
    from raw
),
joined as (
    select d.Dummy as Dummy, f.Dummy as FirstDummy, s.Dummy as SecondDummy
    from #system.dual() d
    inner join firstCte f on d.Dummy = f.Dummy
    inner join secondCte s on d.Dummy = s.Dummy
)
select j.Dummy, j.FirstDummy, j.SecondDummy
from joined j
semi join eligible e on j.Dummy = e.Dummy";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("-> KeySet", result.PlanningText);
        AssertExecutionPlanContains("KeySetProbe [eKeys[d.Dummy]]", result.ExecutionPlanText);
        AssertTextBefore("KeySetProbe [eKeys[d.Dummy]]", "HashProbe [dfFHash[d.Dummy]", result.ExecutionPlanText);
        Assert.IsTrue(
            result.GeneratedCSharpCode.IndexOf("if (eKeys.Contains(", StringComparison.Ordinal) <
            result.GeneratedCSharpCode.IndexOf(".TryGetValue(", StringComparison.Ordinal),
            result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteIsConsumedByLaterCteAndOuterQuery_ShouldReuseOneSidecar()
    {
        const string query = @"
with indexed as (
    select d.Dummy as Dummy
    from #system.dual() d
),
later as (
    select d.Dummy as Dummy
    from #system.dual() d
    inner join indexed i on d.Dummy = i.Dummy
)
select l.Dummy, i.Dummy
from later l
inner join indexed i on l.Dummy = i.Dummy";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("slot 0 allocated", result.PlanningText);
        Assert.Contains("slot 0 reused", result.PlanningText);
        Assert.AreEqual(1, CountOccurrences(result.ExecutionPlanText, "StoreCteIndex ["));
        Assert.AreEqual(2, CountOccurrences(result.ExecutionPlanText, "LoadCteIndex ["));
        AssertExecutionPlanDoesNotContain("HashAdd [iHash", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenPayloadFreeSemiJoinConsumesCte_ShouldUseKeySetSidecar()
    {
        var result = Inspect(CreateCteBackedSemiJoinQuery(), CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> KeySet", result.PlanningText);
        AssertExecutionPlanContains("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanContains("LoadCteIndex [rKeys <- _cteIndexResults.Slot", result.ExecutionPlanText);
        AssertExecutionPlanContains("KeySetProbe [rKeys[l.Dummy]]", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("CreateKeySet [rKeys:", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("KeySetAdd [rKeys", result.ExecutionPlanText);
        AssertGeneratedCSharpUsesTypedCteIndexResults(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenPayloadFreeAntiSemiJoinConsumesCte_ShouldUseKeySetSidecar()
    {
        var result = Inspect(CreateCteBackedAntiSemiJoinQuery(), CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> KeySet", result.PlanningText);
        AssertExecutionPlanContains("LoadCteIndex [rKeys <- _cteIndexResults.Slot", result.ExecutionPlanText);
        AssertExecutionPlanContains("KeySetProbe [rKeys[l.Dummy]]", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("CreateKeySet [rKeys:", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("KeySetAdd [rKeys", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSemiJoinHasResidualPredicate_ShouldUseHashSidecar()
    {
        const string query = @"
with leftCte as (
    select d.Dummy as Dummy
    from #system.dual() d
),
rightCte as (
    select e.Dummy as Dummy
    from #system.dual() e
)
select l.Dummy
from leftCte l
semi join rightCte r on l.Dummy = r.Dummy and r.Dummy like 'single%'";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("-> Hash", result.PlanningText);
        Assert.IsFalse(result.PlanningText.Contains("-> KeySet", StringComparison.Ordinal));
        AssertExecutionPlanContains("LoadCteIndex [rHash <- _cteIndexResults.Slot", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [rKeys <- _cteIndexResults.Slot", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteBuildKeyIsNotSimpleOutputColumn_ShouldSkipSidecar()
    {
        const string query = @"
with leftCte as (
    select d.Dummy as Dummy
    from #system.dual() d
),
rightCte as (
    select e.Dummy as Dummy
    from #system.dual() e
)
select l.Dummy, r.Dummy
from leftCte l
inner join rightCte r on l.Dummy = r.Dummy + ''";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> Skipped", result.PlanningText);
        AssertExecutionPlanDoesNotContain("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenJoinIsNestedLoop_ShouldSkipSidecar()
    {
        var result = Inspect(CreateCteBackedInnerNestedLoopJoinQuery(), CreateCteSidecarOptions());

        AssertExecutionPlanDoesNotContain("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [l in _cteRowResults.Slot0]", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [r in _cteRowResults.Slot1]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenJoinIsSortMerge_ShouldSkipSidecar()
    {
        const string query = @"
with rightCte as (
    select e.Dummy as Dummy
    from #system.dual() e
)
select d.Dummy, r.Dummy
from #system.dual() d
inner join rightCte r on d.Dummy > r.Dummy";

        var result = Inspect(
            query,
            new CompilationOptions(
                parallelizationMode: ParallelizationMode.None,
                useHashJoin: true,
                useSortMergeJoin: true,
                useCteSidecarIndexes: true));

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("Sort-merge joins keep their existing range/order-oriented lowering.", result.PlanningText);
        AssertExecutionPlanDoesNotContain("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteOutputRequiresFinalRewrite_ShouldSkipSidecar()
    {
        const string query = @"
with rightCte as (
    select distinct e.Dummy as Dummy
    from #system.dual() e
)
select d.Dummy, r.Dummy
from #system.dual() d
inner join rightCte r on d.Dummy = r.Dummy";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> Skipped", result.PlanningText);
        AssertExecutionPlanDoesNotContain("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteOutputHasPostOperationRewrite_ShouldSkipSidecar()
    {
        const string query = @"
with rightCte as (
    select e.Dummy as Dummy
    from #system.dual() e
    order by e.Dummy
)
select d.Dummy, r.Dummy
from #system.dual() d
inner join rightCte r on d.Dummy = r.Dummy";

        var result = Inspect(query, CreateCteSidecarOptions());

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("final post-operations", result.PlanningText);
        AssertExecutionPlanDoesNotContain("StoreCteIndex [", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("LoadCteIndex [", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteHashSidecarIsEnabled_ShouldMatchDefaultExecution()
    {
        var baseline = CompileForExecution(CreateCteBackedInnerHashJoinQuery(), CreateCteSidecarDisabledOptions()).Run();
        var optimized = CompileForExecution(CreateCteBackedInnerHashJoinQuery(), CreateCteSidecarOptions()).Run();

        Assert.AreEqual(baseline.Count, optimized.Count);
        Assert.AreEqual(baseline[0][0], optimized[0][0]);
        Assert.AreEqual(baseline[0][1], optimized[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteResidualOuterHashSidecarIsEnabled_ShouldMatchDefaultExecution()
    {
        var baseline = CompileForExecution(CreateCteBackedResidualOuterHashJoinQuery(), CreateCteSidecarDisabledOptions()).Run();
        var optimized = CompileForExecution(CreateCteBackedResidualOuterHashJoinQuery(), CreateCteSidecarOptions()).Run();

        Assert.AreEqual(baseline.Count, optimized.Count);
        Assert.AreEqual(baseline[0][0], optimized[0][0]);
        Assert.AreEqual(baseline[0][1], optimized[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteKeySetSidecarIsEnabled_ShouldMatchSemiAndAntiExecution()
    {
        var semiBaseline = CompileForExecution(CreateCteBackedSemiJoinQuery(), CreateCteSidecarDisabledOptions()).Run();
        var semiOptimized = CompileForExecution(CreateCteBackedSemiJoinQuery(), CreateCteSidecarOptions()).Run();
        var antiBaseline = CompileForExecution(CreateCteBackedAntiSemiJoinQuery(), CreateCteSidecarDisabledOptions()).Run();
        var antiOptimized = CompileForExecution(CreateCteBackedAntiSemiJoinQuery(), CreateCteSidecarOptions()).Run();

        Assert.AreEqual(semiBaseline.Count, semiOptimized.Count);
        Assert.AreEqual(semiBaseline[0][0], semiOptimized[0][0]);
        Assert.AreEqual(antiBaseline.Count, antiOptimized.Count);
    }

    private static CompilationOptions CreateCteSidecarOptions()
    {
        return new CompilationOptions(
            parallelizationMode: ParallelizationMode.None,
            useHashJoin: true,
            useSortMergeJoin: false,
            useCteParallelization: false,
            useCteSidecarIndexes: true);
    }

    private static CompilationOptions CreateCteSidecarDisabledOptions()
    {
        return new CompilationOptions(
            parallelizationMode: ParallelizationMode.None,
            useHashJoin: true,
            useSortMergeJoin: false,
            useCteParallelization: false,
            useCteSidecarIndexes: false);
    }

    private static void AssertGeneratedCSharpUsesTypedCteIndexResults(string generatedCSharpCode)
    {
        AssertGeneratedCSharpContains("var _cteIndexResults = new CteIndexResults();", generatedCSharpCode);
        AssertGeneratedCSharpContains("private sealed class CteIndexResults", generatedCSharpCode);
        AssertGeneratedCSharpContains("var _cteRowResults = new CteRowResults();", generatedCSharpCode);
        AssertGeneratedCSharpContains("private sealed class CteRowResults", generatedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("private readonly CteIndexResults _cteIndexResults = new CteIndexResults();", generatedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("private readonly CteRowResults _cteRowResults = new CteRowResults();", generatedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("_cteIndexResults = new object[", generatedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("object[] _cteIndexResults", generatedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("Musoq.Evaluator.Tables.Table BuildCte", generatedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("Musoq.Evaluator.Tables.Table[] _tableResults", generatedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("_tableResults[", generatedCSharpCode);
    }

    private static string CreateCteBackedSemiJoinQuery()
    {
        return "with leftCte as (select d.Dummy as Dummy from #system.dual() d), rightCte as (select e.Dummy as Dummy from #system.dual() e) select l.Dummy from leftCte l semi join rightCte r on l.Dummy = r.Dummy";
    }

    private static string CreateCteBackedAntiSemiJoinQuery()
    {
        return "with leftCte as (select d.Dummy as Dummy from #system.dual() d), rightCte as (select e.Dummy as Dummy from #system.dual() e) select l.Dummy from leftCte l anti join rightCte r on l.Dummy = r.Dummy";
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void AssertTextBefore(string expectedEarlier, string expectedLater, string text)
    {
        var earlier = text.IndexOf(expectedEarlier, StringComparison.Ordinal);
        var later = text.IndexOf(expectedLater, StringComparison.Ordinal);

        Assert.IsTrue(earlier >= 0, text);
        Assert.IsTrue(later >= 0, text);
        Assert.IsTrue(earlier < later, text);
    }
}
