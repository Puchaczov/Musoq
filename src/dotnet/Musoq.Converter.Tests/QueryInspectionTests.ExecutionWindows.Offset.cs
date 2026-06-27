using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForLagWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect("select d.Dummy, Lag(d.Dummy, 1) over (order by d.Dummy) as PrevDummy from #system.dual() d",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeLagWindow [resultLags <- resultWindowRows value d.Dummy order by d.Dummy ASC offset 1 default NULL]", result.ExecutionPlanText);
        Assert.Contains("var resultLagsOrderKeys = new WindowResultLagsOrderKeysKey[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("resultLagsValues", result.GeneratedCSharpCode);
        Assert.Contains("var resultLags = new string[resultWindowRows.Count];", result.GeneratedCSharpCode);
        Assert.Contains("resultLagsSourcePartitionIndex >= 0", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeLag", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForPartitionedLagWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'a' as City, 'amy' as Name, 1 as Score from #system.dual()
                    union all (City, Name, Score) select 'a' as City, 'bea' as Name, 2 as Score from #system.dual()
                    union all (City, Name, Score) select 'b' as City, 'cal' as Name, 3 as Score from #system.dual()
                )
                select c.City, c.Score, c.Name, Lag(c.Score, 1) over (partition by c.City order by c.Score) as PrevScore
                from c
                order by c.City, c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("amy", table[0][2]);
        Assert.IsNull(table[0][3]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("bea", table[1][2]);
        Assert.AreEqual(1, table[1][3]);
        Assert.AreEqual("b", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("cal", table[2][2]);
        Assert.IsNull(table[2][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForPartitionedLeadWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'a' as City, 'amy' as Name, 1 as Score from #system.dual()
                    union all (City, Name, Score) select 'a' as City, 'bea' as Name, 2 as Score from #system.dual()
                    union all (City, Name, Score) select 'b' as City, 'cal' as Name, 3 as Score from #system.dual()
                )
                select c.City, c.Score, c.Name, Lead(c.Score, 1) over (partition by c.City order by c.Score) as NextScore
                from c
                order by c.City, c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("amy", table[0][2]);
        Assert.AreEqual(2, table[0][3]);
        Assert.AreEqual("a", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("bea", table[1][2]);
        Assert.IsNull(table[1][3]);
        Assert.AreEqual("b", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("cal", table[2][2]);
        Assert.IsNull(table[2][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedOffsetWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       Lag(c.Score, 1) over (order by c.Score rows between current row and current row) as PrevScore,
                       Lead(c.Score, 1) over (order by c.Score range between unbounded preceding and current row) as NextScore
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeLagWindow [resultLags0 <- resultWindowRows value c.Score order by c.Score ASC offset 1 default NULL]", result.ExecutionPlanText);
        Assert.Contains("ComputeLeadWindow [resultLeads1 <- resultWindowRows value c.Score order by c.Score ASC offset 1 default NULL]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedOffsetWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       c.Score,
                       Lag(c.Score, 1) over (order by c.Score rows between current row and current row) as PrevScore,
                       Lead(c.Score, 1) over (order by c.Score range between unbounded preceding and current row) as NextScore
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.IsNull(table[0][2]);
        Assert.AreEqual(2, table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
        Assert.AreEqual(3, table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual(2, table[2][2]);
        Assert.IsNull(table[2][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRowDependentOffsetWindowArguments_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score, 1 as Step, 'fallback-amy' as Fallback from #system.dual()
                    union all (Name, Score, Step, Fallback) select 'bea' as Name, 2 as Score, 2 as Step, 'fallback-bea' as Fallback from #system.dual()
                )
                select c.Name,
                       Lag(c.Name, c.Step, c.Fallback) over (order by c.Score) as PrevName,
                       Lead(c.Name, c.Step, c.Fallback) over (order by c.Score) as NextName
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeLagWindow [resultLags0 <- resultWindowRows value c.Name order by c.Score ASC offset c.Step default c.Fallback]", result.ExecutionPlanText);
        Assert.Contains("ComputeLeadWindow [resultLeads1 <- resultWindowRows value c.Name order by c.Score ASC offset c.Step default c.Fallback]", result.ExecutionPlanText);
        Assert.Contains("resultLags0Offsets", result.GeneratedCSharpCode);
        Assert.Contains("resultLags0Defaults", result.GeneratedCSharpCode);
        Assert.Contains("resultLeads1Offsets", result.GeneratedCSharpCode);
        Assert.Contains("resultLeads1Defaults", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeLag", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("WindowFunctionHelpers.ComputeLead", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRowDependentOffsetWindowArguments_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score, 1 as Step, 'fallback-amy' as Fallback from #system.dual()
                    union all (Name, Score, Step, Fallback) select 'bea' as Name, 2 as Score, 2 as Step, 'fallback-bea' as Fallback from #system.dual()
                    union all (Name, Score, Step, Fallback) select 'cal' as Name, 3 as Score, 1 as Step, 'fallback-cal' as Fallback from #system.dual()
                )
                  select c.Name,
                      c.Score,
                       Lag(c.Name, c.Step, c.Fallback) over (order by c.Score) as PrevName,
                       Lead(c.Name, c.Step, c.Fallback) over (order by c.Score) as NextName
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("fallback-amy", table[0][2]);
        Assert.AreEqual("bea", table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("fallback-bea", table[1][2]);
        Assert.AreEqual("fallback-bea", table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("bea", table[2][2]);
        Assert.AreEqual("fallback-cal", table[2][3]);
    }

}
