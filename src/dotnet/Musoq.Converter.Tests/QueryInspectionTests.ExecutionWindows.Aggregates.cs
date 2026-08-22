using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForWindowedAggregates_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 'ops' as City, 2 as Score from #system.dual()
                    union all (Name, City, Score) select 'bea' as Name, 'ops' as City, 3 as Score from #system.dual()
                )
                select c.Name,
                       Sum(c.Score) over (partition by c.City order by c.Score) as RunningScore,
                       Count(c.Score) over (partition by c.City) as CityCount
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeSumWindowKernel[BoundedRows] [", result.ExecutionPlanText);
        Assert.Contains("ComputeCountWindowKernel[WholePartition] [", result.ExecutionPlanText);
        Assert.Contains("resultSums0PrefixSum[resultSums0PartitionIndex + 1]", result.GeneratedCSharpCode);
        Assert.Contains("ResolveRangePeerFrameEnd(resultSums0OrderKeys", result.GeneratedCSharpCode);
        Assert.Contains("++resultCounts1Count;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowSum()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowCount()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeTypedPluginWindowFunction<object, decimal>", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeTypedPluginWindowFunction<object, int>", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("RunningScoreValues", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("CityCountValues", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForWindowedAggregates_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 'ops' as City, 2 as Score from #system.dual()
                    union all (Name, City, Score) select 'bea' as Name, 'ops' as City, 3 as Score from #system.dual()
                    union all (Name, City, Score) select 'cal' as Name, 'eng' as City, 5 as Score from #system.dual()
                )
                select c.Name,
                       c.City,
                       c.Score,
                       Sum(c.Score) over (partition by c.City order by c.Score) as RunningScore,
                       Count(c.Score) over (partition by c.City) as CityCount
                from c
                order by c.City, c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("cal", table[0][0]);
        Assert.AreEqual("eng", table[0][1]);
        Assert.AreEqual(5, table[0][2]);
        Assert.AreEqual(5m, table[0][3]);
        Assert.AreEqual(1, table[0][4]);
        Assert.AreEqual("amy", table[1][0]);
        Assert.AreEqual("ops", table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual(2m, table[1][3]);
        Assert.AreEqual(2, table[1][4]);
        Assert.AreEqual("bea", table[2][0]);
        Assert.AreEqual("ops", table[2][1]);
        Assert.AreEqual(3, table[2][2]);
        Assert.AreEqual(5m, table[2][3]);
        Assert.AreEqual(2, table[2][4]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRunningFramedAggregateWindow_ShouldStreamExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       Sum(c.Score) over (
                           order by c.Score
                           rows between unbounded preceding and current row) as RunningScore
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeSumWindowKernel[Running] [", result.ExecutionPlanText);
        Assert.Contains("frame rows between unbounded preceding and current row", result.ExecutionPlanText);
        Assert.Contains("resultSumsSum += (decimal)", result.GeneratedCSharpCode);
        Assert.Contains("] = resultSumsSum;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowSum()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("resultSumsValues", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForWholeFramedAggregateWindow_ShouldStreamExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       Sum(c.Score) over (
                           order by c.Score
                           rows between unbounded preceding and unbounded following) as PartitionScore
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeSumWindowKernel[WholePartition] [", result.ExecutionPlanText);
        Assert.Contains("frame rows between unbounded preceding and unbounded following", result.ExecutionPlanText);
        Assert.Contains("resultSumsSum += (decimal)", result.GeneratedCSharpCode);
        Assert.Contains("resultSumsFinalValue = resultSumsSum;", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowSum()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("resultSumsValues", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedWindowedAggregates_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       Sum(c.Score) over (order by c.Score rows between 1 preceding and current row) as RollingScore
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeSumWindowKernel[BoundedRows] [", result.ExecutionPlanText);
        Assert.Contains("frame rows between 1 preceding and current row", result.ExecutionPlanText);
        Assert.Contains("resultSumsPrefix", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForMinMaxFramedAggregateWindows_ShouldEmitDequeKernels()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 3 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       Min(c.Score) over (order by c.Score rows between 1 preceding and 1 following) as RollingMin,
                       Max(c.Score) over (order by c.Score rows between 1 preceding and 1 following) as RollingMax
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeMinWindowKernel[BoundedRows] [", result.ExecutionPlanText);
        Assert.Contains("ComputeMaxWindowKernel[BoundedRows] [", result.ExecutionPlanText);
        Assert.Contains("resultMins0DequeValues", result.GeneratedCSharpCode);
        Assert.Contains("resultMaxs1DequeValues", result.GeneratedCSharpCode);
        Assert.Contains(".CompareTo(", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowMin()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowMax()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("resultMins0Values", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("resultMaxs1Values", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedWindowedAggregates_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 3 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 4 as Score from #system.dual()
                )
                select c.Name,
                       c.Score,
                       Sum(c.Score) over (order by c.Score rows between 1 preceding and current row) as RollingScore,
                       Avg(c.Score) over (order by c.Score rows between current row and 1 following) as ForwardAverage
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(2m, table[0][2]);
        Assert.AreEqual(2.5m, table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(5m, table[1][2]);
        Assert.AreEqual(3.5m, table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(4, table[2][1]);
        Assert.AreEqual(7m, table[2][2]);
        Assert.AreEqual(4m, table[2][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForMinMaxFramedAggregateWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 'x' as Bucket, 3 as Score from #system.dual()
                    union all (Name, Bucket, Score) select 'bea' as Name, 'x' as Bucket, 1 as Score from #system.dual()
                    union all (Name, Bucket, Score) select 'cal' as Name, 'y' as Bucket, 2 as Score from #system.dual()
                    union all (Name, Bucket, Score) select 'dan' as Name, 'y' as Bucket, 5 as Score from #system.dual()
                )
                select c.Name,
                       Min(c.Score) over (order by c.Bucket, c.Score desc rows between 1 preceding and current row) as RollingMin,
                       Max(c.Score) over (order by c.Bucket, c.Score desc rows between 1 preceding and current row) as RollingMax
                from c
                order by c.Name",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(1, table[1][1]);
        Assert.AreEqual(3, table[1][2]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(2, table[2][1]);
        Assert.AreEqual(5, table[2][2]);
        Assert.AreEqual("dan", table[3][0]);
        Assert.AreEqual(1, table[3][1]);
        Assert.AreEqual(5, table[3][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForPrecedingOnlyFramedAggregateWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       Sum(c.Score) over (order by c.Score rows between 2 preceding and 1 preceding) as PreviousScore
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value c.Score order by c.Score ASC frame rows between 2 preceding and 1 preceding]", result.ExecutionPlanText);
        Assert.Contains("resultSumsPrefix", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForPrecedingOnlyFramedAggregateWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()
                    union all (Name, Score) select 'dan' as Name, 4 as Score from #system.dual()
                )
                select c.Name,
                      c.Score,
                       Sum(c.Score) over (order by c.Score rows between 2 preceding and 1 preceding) as PreviousScore,
                       Count(c.Score) over (order by c.Score rows between 2 preceding and 1 preceding) as PreviousCount
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(0m, table[0][2]);
        Assert.AreEqual(0, table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(1m, table[1][2]);
        Assert.AreEqual(1, table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual(3m, table[2][2]);
        Assert.AreEqual(2, table[2][3]);
        Assert.AreEqual("dan", table[3][0]);
        Assert.AreEqual(4, table[3][1]);
        Assert.AreEqual(5m, table[3][2]);
        Assert.AreEqual(2, table[3][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRangeFramedAggregateWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       Sum(c.Score) over (order by c.Score range between unbounded preceding and current row) as RunningScore
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value c.Score order by c.Score ASC frame range between unbounded preceding and current row]", result.ExecutionPlanText);
        Assert.Contains("resultSumsPrefixSum[resultSumsPartitionIndex + 1]", result.GeneratedCSharpCode);
        Assert.Contains("ResolveRangePeerFrameEnd(resultSumsOrderKeys", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowSum()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".AccumulateValue(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("resultSumsValues", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRangeFramedAggregateWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 3 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 4 as Score from #system.dual()
                )
                select c.Name,
                       c.Score,
                       Sum(c.Score) over (order by c.Score range between unbounded preceding and current row) as RunningScore,
                       Count(c.Score) over (order by c.Score range between current row and unbounded following) as RemainingCount
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(2m, table[0][2]);
        Assert.AreEqual(3, table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(5m, table[1][2]);
        Assert.AreEqual(2, table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(4, table[2][1]);
        Assert.AreEqual(9m, table[2][2]);
        Assert.AreEqual(1, table[2][3]);
    }

}
