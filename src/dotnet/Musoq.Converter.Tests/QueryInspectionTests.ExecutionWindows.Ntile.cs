using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForNtileWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       Ntile(2) over (order by c.Score) as Bucket
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeNtileWindow [", result.ExecutionPlanText);
        Assert.Contains("var resultNtilesBuckets = new int[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("var resultNtiles = new long[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("resultNtilesBucketCount", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowNtile()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetPartitionSize(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".Accumulate(2);", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeTypedPluginWindowFunction<object, long>", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("NtilesValues", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForNtileWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()
                    union all (Name, Score) select 'dan' as Name, 4 as Score from #system.dual()
                    union all (Name, Score) select 'eve' as Name, 5 as Score from #system.dual()
                )
                select c.Name,
                       c.Score,
                       Ntile(3) over (order by c.Score) as Bucket
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual(2L, table[2][2]);
        Assert.AreEqual("dan", table[3][0]);
        Assert.AreEqual(4, table[3][1]);
        Assert.AreEqual(2L, table[3][2]);
        Assert.AreEqual("eve", table[4][0]);
        Assert.AreEqual(5, table[4][1]);
        Assert.AreEqual(3L, table[4][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForNtileQualify_ShouldRunExecutableQuery()
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
                       Ntile(2) over (order by c.Score) as Bucket
                from c
                qualify Ntile(2) over (order by c.Score) = 1
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedNtileWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       Ntile(2) over (order by c.Score rows between current row and current row) as Bucket
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeNtileWindow [resultNtiles <- resultWindowRows value 2 order by c.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("var resultNtilesBuckets = new int[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("resultNtilesBucketCount", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".WindowNtile()", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".AccumulateValue(2);", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputePluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeOrderedFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedNtileWindow_ShouldRunExecutableQuery()
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
                       Ntile(2) over (order by c.Score rows between current row and current row) as Bucket
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual(2L, table[2][2]);
        Assert.AreEqual("dan", table[3][0]);
        Assert.AreEqual(4, table[3][1]);
        Assert.AreEqual(2L, table[3][2]);
    }

}
