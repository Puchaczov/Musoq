using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.RuntimeV2;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CteSidecarIndexPlanningTests
{
    [TestMethod]
    public void CompileForInspection_WhenCteDefinitionUsesParallelFilterProject_ShouldSkipSidecarInPlanning()
    {
        const string query = @"
with indexed as (
    select Id, HeavyComputation(Value) as Heavy
    from #test.entities()
)
select l.Name, i.Heavy
from #test.entities() l
inner join indexed i on l.Id = i.Id";

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new RuntimeV2RegressionSchemaProvider([]),
            new TestsLoggerResolver(),
            new CompilationOptions(
                parallelizationMode: ParallelizationMode.Full,
                useHashJoin: true,
                useSortMergeJoin: false,
                useCteSidecarIndexes: true));

        Assert.Contains("CteSidecarIndexStrategy", result.PlanningText);
        Assert.Contains("-> Skipped", result.PlanningText);
        Assert.Contains("parallel filter/project lowering", result.PlanningText);
        Assert.Contains("ParallelFilterProjectLoop [ko3iko in cte0_ko3ikoRows", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreCteIndex [", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("LoadCteIndex [", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteIndexResults", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenSidecarCteUsesSubsetOfUpstreamColumns_ShouldPruneInternalPayload()
    {
        const string query = @"
with raw as (
    select Id, Name, City, Country, Population
    from #A.entities()
),
eligible as (
    select Id
    from raw
    where Population > 0
)
select b.Name
from #B.entities() b
semi join eligible e on b.Id = e.Id";

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    { "#A", Array.Empty<BasicEntity>() },
                    { "#B", Array.Empty<BasicEntity>() }
                }),
            new TestsLoggerResolver(),
            new CompilationOptions(
                useHashJoin: true,
                useSortMergeJoin: false,
                useCteParallelization: false,
                useCteSidecarIndexes: true));

        var rawRow = ExtractGeneratedClass(result.GeneratedCSharpCode, "Cte0Row0");
        Assert.Contains("public int Id", rawRow);
        Assert.Contains("public decimal Population", rawRow);
        Assert.IsFalse(rawRow.Contains("Country", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(rawRow.Contains("public string Name", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(rawRow.Contains("public string City", StringComparison.Ordinal), result.GeneratedCSharpCode);

        Assert.Contains("public HashSet<int> Slot0;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class Cte1Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("List<Cte1Row0>", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot1", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenSiblingSidecarCtesReadSameSafeSource_ShouldFuseInputLoop()
    {
        const string query = @"
with raw as (
    select Id, Name, City, Population
    from #A.entities()
),
names as (
    select Id, Name
    from raw
),
cities as (
    select Id, City
    from raw
),
eligible as (
    select Id
    from raw
    where Population > 0
)
select b.Name, n.Name, c.City
from #B.entities() b
inner join names n on b.Id = n.Id
inner join cities c on b.Id = c.Id
semi join eligible e on b.Id = e.Id";

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    { "#A", Array.Empty<BasicEntity>() },
                    { "#B", Array.Empty<BasicEntity>() }
                }),
            new TestsLoggerResolver(),
            new CompilationOptions(
                useHashJoin: true,
                useSortMergeJoin: false,
                useCteParallelization: false,
                useCteSidecarIndexes: true));

        Assert.Contains("FusedCteProducer [cte1 -> sidecar-only, cte2 -> sidecar-only, cte3 -> sidecar-only]", result.ExecutionPlanText);
        Assert.Contains("ForEach [ko3iko in cte0_ko3ikoRows]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ForEach [raw in CastGeneratedRows<Cte0Row0>", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateTable [cte0:", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte0 ->", StringComparison.Ordinal), result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte0Row0> BuildCte0(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte1Row0> BuildCte1(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte2Row0> BuildCte2(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private static List<Cte3Row0> BuildCte3(", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.Contains("_cteIndexResults.Slot0 = cte1HashSidecar0Id;", result.GeneratedCSharpCode);
        Assert.Contains("_cteIndexResults.Slot1 = cte2HashSidecar1Id;", result.GeneratedCSharpCode);
        Assert.Contains("_cteIndexResults.Slot2 = cte3KeySetSidecar2Id;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot1", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot2", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_cteRowResults.Slot3", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("List<Cte1Row0>", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("List<Cte2Row0>", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte0Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Cte3Row0", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    private static string ExtractGeneratedClass(string generatedCode, string className)
    {
        var marker = $"private sealed class {className}";
        var start = generatedCode.IndexOf(marker, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, generatedCode);

        var next = generatedCode.IndexOf("private sealed class ", start + marker.Length, StringComparison.Ordinal);
        return next < 0 ? generatedCode[start..] : generatedCode[start..next];
    }

}
