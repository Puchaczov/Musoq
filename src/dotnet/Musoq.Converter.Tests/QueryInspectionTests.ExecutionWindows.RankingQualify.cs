using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRowNumberQualify_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect("select d.Dummy from #system.dual() d qualify RowNumber() over (order by d.Dummy) <= 1",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by d.Dummy ASC qualify <= 1]", result.ExecutionPlanText);
        Assert.Contains("If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 1))]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForPartitionedRowNumberQualify_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'a' as City, 2 as Score from #system.dual()
                    union all (City, Score) select 'a' as City, 1 as Score from #system.dual()
                    union all (City, Score) select 'b' as City, 3 as Score from #system.dual()
                )
                select c.City, c.Score
                from c
                qualify RowNumber() over (partition by c.City order by c.Score) <= 1
                order by c.City",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("b", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRankWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect("select d.Dummy, Rank() over (order by d.Dummy) as rn from #system.dual() d",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRankWindow [resultRanks <- resultWindowRows order by d.Dummy ASC]", result.ExecutionPlanText);
        Assert.Contains("var resultRanks = new long[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("long resultRanksRank = 1L;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeRank", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForSinglePartitionedIntRankWindow_ShouldUseTypedIntOrderBuilder()
    {
        var result = Inspect(@"
                with c as (
                    select 'a' as City, 2 as Score from #system.dual()
                    union all (City, Score) select 'a' as City, 1 as Score from #system.dual()
                    union all (City, Score) select 'b' as City, 3 as Score from #system.dual()
                )
                select c.City, Rank() over (partition by c.City order by c.Score desc) as Rnk
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRankWindow [resultRanks <- resultWindowRows partition by c.City order by c.Score DESC]", result.ExecutionPlanText);
        Assert.Contains("WindowIntOrderBuilder<string>", result.GeneratedCSharpCode);
        Assert.Contains("resultRanksIntOrderBuilder.Add((string)(string)c[0], (int)(int)c[1], windowIndex);", result.GeneratedCSharpCode);
        Assert.Contains("var resultRanksPartitions = resultRanksIntOrderBuilder.ToSortedPartitionSet(true);", result.GeneratedCSharpCode);
        Assert.Contains("var resultRanks = resultRanksIntOrderBuilder.ComputeRank(resultRanksPartitions);", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("resultRanksOrderKeys", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeRank", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForSinglePartitionedIntDenseRankWindow_ShouldUseTypedIntOrderBuilder()
    {
        var result = Inspect(@"
                with c as (
                    select 'a' as City, 2 as Score from #system.dual()
                    union all (City, Score) select 'a' as City, 1 as Score from #system.dual()
                    union all (City, Score) select 'b' as City, 3 as Score from #system.dual()
                )
                select c.City, DenseRank() over (partition by c.City order by c.Score desc) as Rnk
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeDenseRankWindow [resultDenseRanks <- resultWindowRows partition by c.City order by c.Score DESC]", result.ExecutionPlanText);
        Assert.Contains("WindowIntOrderBuilder<string>", result.GeneratedCSharpCode);
        Assert.Contains("resultDenseRanksIntOrderBuilder.Add((string)(string)c[0], (int)(int)c[1], windowIndex);", result.GeneratedCSharpCode);
        Assert.Contains("var resultDenseRanksPartitions = resultDenseRanksIntOrderBuilder.ToSortedPartitionSet(true);", result.GeneratedCSharpCode);
        Assert.Contains("var resultDenseRanks = resultDenseRanksIntOrderBuilder.ComputeDenseRank(resultDenseRanksPartitions);", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("resultDenseRanksOrderKeys", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeDenseRank", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRankWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 10 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 10 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 5 as Score from #system.dual()
                )
                select c.Name, Rank() over (order by c.Score desc) as Rnk
                from c
                order by c.Name",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3L, table[2][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForDenseRankQualify_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 10 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 10 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 5 as Score from #system.dual()
                )
                select c.Name
                from c
                qualify DenseRank() over (order by c.Score desc) <= 1
                order by c.Name",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual("bea", table[1][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForMultiOrderRowNumberWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'a' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'b' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'c' as Name, 2 as Score from #system.dual()
                )
                select c.Name, RowNumber() over (order by c.Score, c.Name desc) as Rn
                from c
                order by Rn",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("b", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual("c", table[2][0]);
        Assert.AreEqual(3L, table[2][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFilteredRowNumberWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'a' as Name, 0 as Score from #system.dual()
                    union all (Name, Score) select 'b' as Name, 1 as Score from #system.dual()
                )
                select c.Name, RowNumber() over (order by c.Score) as Rn
                from c
                where c.Score > 0",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("MaterializeFiltered [_cteRowResults.Slot0 where (c.Score > 0) -> resultWindowRows]", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by c.Score ASC]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFilteredRankWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 0 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 10 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 10 as Score from #system.dual()
                    union all (Name, Score) select 'dee' as Name, 5 as Score from #system.dual()
                )
                select c.Name, Rank() over (order by c.Score desc) as Rnk
                from c
                where c.Score > 0
                order by c.Name",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("bea", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("cal", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
        Assert.AreEqual("dee", table[2][0]);
        Assert.AreEqual(3L, table[2][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedRankingWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       RowNumber() over (order by c.Score rows between current row and current row) as Rn,
                       Rank() over (order by c.Score desc range between unbounded preceding and current row) as Rnk
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows order by c.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("ComputeRankWindow [resultRanks1 <- resultWindowRows order by c.Score DESC]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedRankingWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       c.Score,
                       RowNumber() over (order by c.Score rows between current row and current row) as Rn,
                       DenseRank() over (order by c.Score desc range between unbounded preceding and current row) as Rnk
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual(3L, table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual(3L, table[2][2]);
        Assert.AreEqual(1L, table[2][3]);
    }

}
