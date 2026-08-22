using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRowNumberWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect("select d.Dummy, RowNumber() over (order by d.Dummy) as rn from #system.dual() d",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by d.Dummy ASC]", result.ExecutionPlanText);
        Assert.Contains("var resultRowNumbers = new long[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("resultRowNumbers[resultRowNumbersCurrentIndex] = resultRowNumbersPartitionIndex + 1L;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeRowNumber", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderRowNumberWindow_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy, RowNumber() over (order by d.Dummy) as rn from #system.dual() d");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("MaterializeChunked [dRows -> resultWindowRows]", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by d.Dummy ASC]", result.ExecutionPlanText);
        Assert.Contains("var resultRowNumbers = new long[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("resultRowNumbers[resultRowNumbersCurrentIndex] = resultRowNumbersPartitionIndex + 1L;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeRowNumber", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRowNumberWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select d.Dummy, RowNumber() over (order by d.Dummy) as rn from #system.dual() d",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForSharedRankingWindows_ShouldFuseKernelPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       RowNumber() over (order by c.Score) as Rn,
                       Rank() over (order by c.Score) as Rnk,
                       DenseRank() over (order by c.Score) as Drnk
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("WindowKernelPlan [hash partition/per-partition sort; kernels 3;", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows order by c.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("ComputeRankWindow [resultRanks1 <- resultWindowRows order by c.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("ComputeDenseRankWindow [resultDenseRanks2 <- resultWindowRows order by c.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("for (int resultRowNumbers0WindowPlanPartitionSetIndex", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("for (int resultRanks1PartitionSetIndex", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("for (int resultDenseRanks2PartitionSetIndex", StringComparison.Ordinal));
        Assert.AreEqual(1, CountOccurrences(result.GeneratedCSharpCode, "SortStructPartitionSet"));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForSharedRankingWindows_ShouldRunFusedKernelPlan()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       RowNumber() over (order by c.Score) as Rn,
                       Rank() over (order by c.Score) as Rnk,
                       DenseRank() over (order by c.Score) as Drnk
                from c
                order by c.Name",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
        Assert.AreEqual(1L, table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3L, table[2][1]);
        Assert.AreEqual(3L, table[2][2]);
        Assert.AreEqual(2L, table[2][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForGeneratedMultiKeyRankPeers_ShouldCompareAllOrderParts()
    {
        var provider = new DynamicRowsSchemaProvider(
            new Dictionary<string, Type>
            {
                ["Id"] = typeof(string),
                ["Name"] = typeof(string),
                ["Score"] = typeof(int)
            },
            new List<IReadOnlyDictionary<string, object>>
            {
                new Dictionary<string, object> { ["Id"] = "a", ["Name"] = "amy", ["Score"] = 1 },
                new Dictionary<string, object> { ["Id"] = "b", ["Name"] = "amy", ["Score"] = 1 },
                new Dictionary<string, object> { ["Id"] = "c", ["Name"] = "bea", ["Score"] = 1 },
                new Dictionary<string, object> { ["Id"] = "d", ["Name"] = "dee", ["Score"] = 2 }
            });
        var compiled = CompileForExecution(@"
                select d.Id,
                       Rank() over (order by d.Score, d.Name desc) as Rnk,
                       DenseRank() over (order by d.Score, d.Name desc) as Drnk
                from #dynamic.all() d
                order by d.Id",
            provider,
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual(2L, table[0][2]);
        Assert.AreEqual("b", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
        Assert.AreEqual("c", table[2][0]);
        Assert.AreEqual(1L, table[2][1]);
        Assert.AreEqual(1L, table[2][2]);
        Assert.AreEqual("d", table[3][0]);
        Assert.AreEqual(4L, table[3][1]);
        Assert.AreEqual(3L, table[3][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForGeneratedNullableRankPeers_ShouldTreatNullsAsPeers()
    {
        var provider = new DynamicRowsSchemaProvider(
            new Dictionary<string, Type>
            {
                ["Id"] = typeof(string),
                ["Score"] = typeof(int?)
            },
            new List<IReadOnlyDictionary<string, object>>
            {
                new Dictionary<string, object> { ["Id"] = "a", ["Score"] = null! },
                new Dictionary<string, object> { ["Id"] = "b", ["Score"] = null! },
                new Dictionary<string, object> { ["Id"] = "c", ["Score"] = 1 },
                new Dictionary<string, object> { ["Id"] = "d", ["Score"] = 2 }
            });
        var compiled = CompileForExecution(@"
                select d.Id,
                       Rank() over (order by d.Score) as Rnk,
                       DenseRank() over (order by d.Score) as Drnk
                from #dynamic.all() d
                order by d.Id",
            provider,
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("b", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
        Assert.AreEqual("c", table[2][0]);
        Assert.AreEqual(3L, table[2][1]);
        Assert.AreEqual(2L, table[2][2]);
        Assert.AreEqual("d", table[3][0]);
        Assert.AreEqual(4L, table[3][1]);
        Assert.AreEqual(3L, table[3][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForMultipleRankingSpecs_ShouldFuseOnlySharedSpec()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       RowNumber() over (order by c.Score) as ScoreRow,
                       Rank() over (order by c.Score) as ScoreRank,
                       RowNumber() over (order by c.Name) as NameRow
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("WindowKernelPlan [hash partition/per-partition sort; kernels 2;", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers2 <- resultWindowRows order by c.Name ASC]", result.ExecutionPlanText);
        Assert.Contains("for (int resultRowNumbers0WindowPlanPartitionSetIndex", result.GeneratedCSharpCode);
        Assert.Contains("for (int resultRowNumbers2PartitionSetIndex", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForQualifySharedRankingSpec_ShouldReuseFusedPlan()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       RowNumber() over (order by c.Score) as Rn,
                       Rank() over (order by c.Score) as Rnk
                from c
                qualify RowNumber() over (order by c.Score) <= 2
                order by Rn",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForCteRowNumberWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "with c as (select d.Dummy as Dummy from #system.dual() d) select c.Dummy, RowNumber() over (order by c.Dummy) as rn from c",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForPartitionedRowNumberWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect("select d.Dummy, RowNumber() over (partition by d.Dummy order by d.Dummy) as rn from #system.dual() d",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows partition by d.Dummy order by d.Dummy ASC]", result.ExecutionPlanText);
        Assert.Contains("resultRowNumbersPartitionKeys", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForPartitionedRowNumberWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'a' as City, 2 as Score from #system.dual()
                    union all (City, Score) select 'a' as City, 1 as Score from #system.dual()
                    union all (City, Score) select 'b' as City, 3 as Score from #system.dual()
                )
                select c.City, c.Score, RowNumber() over (partition by c.City order by c.Score) as rn
                from c
                order by c.City, c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
        Assert.AreEqual("b", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual(1L, table[2][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForDynamicRowNumberWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect("select d.Team, d.Name, d.Score, RowNumber() over (partition by d.Team order by d.Score) as rn from #dynamic.all() d",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ExpandoAdapter [d: dDynamicRow0]", result.ExecutionPlanText);
        AssertExecutionPlanContains("MaterializeChunkedExpando [dRows as dDynamicRow0 -> resultWindowRows]", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows partition by d.Team order by d.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("new List<dDynamicRow0>", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForDynamicRowNumberWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                select d.Team, d.Name, d.Score, RowNumber() over (partition by d.Team order by d.Score) as rn
                from #dynamic.all() d
                where d.Score > 0
                order by d.Team, d.Score",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual("bea", table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual("ada", table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
        Assert.AreEqual("b", table[2][0]);
        Assert.AreEqual("cid", table[2][1]);
        Assert.AreEqual(3, table[2][2]);
        Assert.AreEqual(1L, table[2][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForDynamicPluginWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                select d.Team,
                       d.Name,
                       d.Score,
                       Sum(d.Score) over (partition by d.Team order by d.Score) as RunningScore,
                       FirstValue(d.Name) over (partition by d.Team order by d.Score) as FirstName
                from #dynamic.all() d",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        AssertExecutionPlanContains("MaterializeChunkedExpando [dRows as dDynamicRow0 -> resultWindowRows]", result.ExecutionPlanText);
        Assert.Contains("ComputeSumWindowKernel[BoundedRows] [", result.ExecutionPlanText);
        Assert.Contains("value d.Score partition by d.Team order by d.Score ASC", result.ExecutionPlanText);
        Assert.Contains("ComputeFirstValueWindow [", result.ExecutionPlanText);
        Assert.Contains("value d.Name partition by d.Team order by d.Score ASC", result.ExecutionPlanText);
        Assert.Contains("resultFirstValues1Values", result.GeneratedCSharpCode);
        Assert.Contains("resultFirstValues1SourcePartitionIndex", result.GeneratedCSharpCode);
        Assert.Contains("resultSums0PrefixSum[resultSums0PartitionIndex + 1]", result.GeneratedCSharpCode);
        Assert.Contains("ResolveRangePeerFrameEnd(resultSums0OrderKeys", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowSum()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowFirstValue()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".AccumulateValue(d.Name);", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputePluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForDynamicPluginWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                select d.Team,
                       d.Name,
                       d.Score,
                       Sum(d.Score) over (partition by d.Team order by d.Score) as RunningScore,
                       Count(d.Score) over (partition by d.Team) as TeamCount,
                       FirstValue(d.Name) over (partition by d.Team order by d.Score) as FirstName
                from #dynamic.all() d
                order by d.Team, d.Score",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual("bea", table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(1m, table[0][3]);
        Assert.AreEqual(2, table[0][4]);
        Assert.AreEqual("bea", table[0][5]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual("ada", table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual(3m, table[1][3]);
        Assert.AreEqual(2, table[1][4]);
        Assert.AreEqual("bea", table[1][5]);
        Assert.AreEqual("b", table[2][0]);
        Assert.AreEqual("cid", table[2][1]);
        Assert.AreEqual(3, table[2][2]);
        Assert.AreEqual(3m, table[2][3]);
        Assert.AreEqual(1, table[2][4]);
        Assert.AreEqual("cid", table[2][5]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForCteDynamicRowNumberWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with sourceRows as (
                    select d.Team as Team, d.Name as Name, d.Score as Score
                    from #dynamic.all() d
                )
                select sourceRows.Team,
                       sourceRows.Name,
                       sourceRows.Score,
                       RowNumber() over (partition by sourceRows.Team order by sourceRows.Score) as rn
                from sourceRows",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("Materialize [_cteRowResults.Slot0 -> resultWindowRows]", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows partition by sourceRows.Team order by sourceRows.Score ASC]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForCteDynamicRowNumberWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with sourceRows as (
                    select d.Team as Team, d.Name as Name, d.Score as Score
                    from #dynamic.all() d
                )
                select sourceRows.Team,
                       sourceRows.Name,
                       sourceRows.Score,
                       RowNumber() over (partition by sourceRows.Team order by sourceRows.Score) as rn
                from sourceRows
                order by sourceRows.Team, sourceRows.Score",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual("bea", table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual("ada", table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
        Assert.AreEqual("b", table[2][0]);
        Assert.AreEqual("cid", table[2][1]);
        Assert.AreEqual(3, table[2][2]);
        Assert.AreEqual(1L, table[2][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForDynamicRowNumberWindowInsideCte_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with rankedRows as (
                    select d.Team as Team,
                           d.Name as Name,
                           d.Score as Score,
                           RowNumber() over (partition by d.Team order by d.Score) as rn
                    from #dynamic.all() d
                )
                select rankedRows.Team, rankedRows.Name, rankedRows.Score, rankedRows.rn
                from rankedRows",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        AssertExecutionPlanContains("MaterializeChunkedExpando [dRows as dDynamicRow0 -> windowRows]", result.ExecutionPlanText);
        AssertExecutionPlanContains("ComputeRowNumberWindow [rowNumbers <- windowRows partition by d.Team order by d.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForDynamicRowNumberWindowInsideCte_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with rankedRows as (
                    select d.Team as Team,
                           d.Name as Name,
                           d.Score as Score,
                           RowNumber() over (partition by d.Team order by d.Score) as rn
                    from #dynamic.all() d
                )
                select rankedRows.Team, rankedRows.Name, rankedRows.Score, rankedRows.rn
                from rankedRows
                order by rankedRows.Team, rankedRows.Score",
            CreateDynamicRowsSchemaProvider(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual("bea", table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual("ada", table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
        Assert.AreEqual("b", table[2][0]);
        Assert.AreEqual("cid", table[2][1]);
        Assert.AreEqual(3, table[2][2]);
        Assert.AreEqual(1L, table[2][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForJoinedRowNumberWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect("select d.Dummy, e.Dummy, RowNumber() over (order by d.Dummy) as rn from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("StoreTable [statement0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("Materialize [_cteRowResults.Slot0 -> resultWindowRows]", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by de.d.Dummy ASC]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForJoinedRowNumberWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select d.Dummy, e.Dummy, RowNumber() over (order by d.Dummy) as rn from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual("single", table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
    }

}
