using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForMultipleWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       RowNumber() over (order by c.Score) as Rn,
                       Lag(c.Score, 1) over (order by c.Score) as PrevScore
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows order by c.Score ASC]", result.ExecutionPlanText);
        Assert.Contains("ComputeLagWindow [resultLags1 <- resultWindowRows value c.Score order by c.Score ASC offset 1 default NULL]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForMultipleWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       RowNumber() over (order by c.Score) as Rn,
                       Lag(c.Score, 1) over (order by c.Score) as PrevScore
                from c
                order by Rn",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.IsNull(table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3L, table[2][1]);
        Assert.AreEqual(2, table[2][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForValueWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       FirstValue(c.Name) over (order by c.Score) as FirstName,
                       NthValue(c.Name, 2) over (order by c.Score) as SecondName
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeFirstValueWindow [resultFirstValues0 <- resultWindowRows value c.Name order by c.Score ASC frame range between unbounded preceding and current row]", result.ExecutionPlanText);
        Assert.Contains("ComputeNthValueWindow [resultNthValues1 <- resultWindowRows value c.Name order by c.Score ASC frame range between unbounded preceding and current row args 2]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForValueWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       c.Score,
                       FirstValue(c.Name) over (order by c.Score) as FirstName,
                       LastValue(c.Name) over (order by c.Score) as LastName,
                       NthValue(c.Name, 2) over (order by c.Score) as SecondName
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("amy", table[0][2]);
        Assert.AreEqual("amy", table[0][3]);
        Assert.IsNull(table[0][4]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("amy", table[1][2]);
        Assert.AreEqual("bea", table[1][3]);
        Assert.AreEqual("bea", table[1][4]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("amy", table[2][2]);
        Assert.AreEqual("cal", table[2][3]);
        Assert.AreEqual("bea", table[2][4]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRowDependentValueWindowArguments_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score, 1 as Position from #system.dual()
                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 2 as Position from #system.dual()
                )
                select c.Name,
                       NthValue(c.Name, c.Position) over (order by c.Score) as PickedName
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeNthValueWindow [resultNthValues <- resultWindowRows value c.Name order by c.Score ASC frame range between unbounded preceding and current row args c.Position]", result.ExecutionPlanText);
        Assert.Contains("resultNthValuesArguments", result.GeneratedCSharpCode);
        Assert.Contains("resultNthValuesSourcePartitionIndex", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputePluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRowDependentValueWindowArguments_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score, 1 as Position from #system.dual()
                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 2 as Position from #system.dual()
                    union all (Name, Score, Position) select 'cal' as Name, 3 as Score, 1 as Position from #system.dual()
                )
                select c.Name,
                       c.Score,
                       NthValue(c.Name, c.Position) over (order by c.Score) as PickedName
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("amy", table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("bea", table[1][2]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("amy", table[2][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedValueWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select c.Name,
                       FirstValue(c.Name) over (order by c.Score rows between 1 preceding and 1 following) as FirstName,
                       NthValue(c.Name, 2) over (order by c.Score rows between 1 preceding and 1 following) as SecondName
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeFirstValueWindow [resultFirstValues0 <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 preceding and 1 following]", result.ExecutionPlanText);
        Assert.Contains("ComputeNthValueWindow [resultNthValues1 <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 preceding and 1 following args 2]", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedValueWindows_ShouldRunExecutableQuery()
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
                       FirstValue(c.Name) over (order by c.Score rows between 1 preceding and 1 following) as FirstName,
                       LastValue(c.Name) over (order by c.Score rows between 1 preceding and 1 following) as LastName,
                       NthValue(c.Name, 2) over (order by c.Score rows between 1 preceding and 1 following) as SecondName
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("amy", table[0][2]);
        Assert.AreEqual("bea", table[0][3]);
        Assert.AreEqual("bea", table[0][4]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("amy", table[1][2]);
        Assert.AreEqual("cal", table[1][3]);
        Assert.AreEqual("bea", table[1][4]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("bea", table[2][2]);
        Assert.AreEqual("dan", table[2][3]);
        Assert.AreEqual("cal", table[2][4]);
        Assert.AreEqual("dan", table[3][0]);
        Assert.AreEqual(4, table[3][1]);
        Assert.AreEqual("cal", table[3][2]);
        Assert.AreEqual("dan", table[3][3]);
        Assert.AreEqual("dan", table[3][4]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedRowDependentValueWindowArguments_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score, 2 as Position from #system.dual()
                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 3 as Position from #system.dual()
                )
                select c.Name,
                       NthValue(c.Name, c.Position) over (order by c.Score rows between 1 preceding and 1 following) as PickedName
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeNthValueWindow [resultNthValues <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 preceding and 1 following args c.Position]", result.ExecutionPlanText);
        Assert.Contains("resultNthValuesArguments", result.GeneratedCSharpCode);
        Assert.Contains("resultNthValuesFrameStart", result.GeneratedCSharpCode);
        Assert.Contains("resultNthValuesSourcePartitionIndex", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedRowDependentValueWindowArguments_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score, 2 as Position from #system.dual()
                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 3 as Position from #system.dual()
                    union all (Name, Score, Position) select 'cal' as Name, 3 as Score, 1 as Position from #system.dual()
                    union all (Name, Score, Position) select 'dan' as Name, 4 as Score, 2 as Position from #system.dual()
                )
                select c.Name,
                       c.Score,
                       NthValue(c.Name, c.Position) over (order by c.Score rows between 1 preceding and 1 following) as PickedName
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("bea", table[0][2]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("cal", table[1][2]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("bea", table[2][2]);
        Assert.AreEqual("dan", table[3][0]);
        Assert.AreEqual(4, table[3][1]);
        Assert.AreEqual("dan", table[3][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRangeFramedValueWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select FirstValue(c.Name) over (order by c.Score range between 1 preceding and 1 following) as FirstName
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeFirstValueWindow [resultFirstValues <- resultWindowRows value c.Name order by c.Score ASC frame range between 1 preceding and 1 following]", result.ExecutionPlanText);
        Assert.Contains("resultFirstValuesFrameStart", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRangeFramedValueWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()
                )
                select c.Name,
                       c.Score,
                       FirstValue(c.Name) over (order by c.Score range between 1 preceding and 1 following) as FirstName,
                       LastValue(c.Name) over (order by c.Score range between current row and unbounded following) as LastName
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("amy", table[0][2]);
        Assert.AreEqual("cal", table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("amy", table[1][2]);
        Assert.AreEqual("cal", table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("bea", table[2][2]);
        Assert.AreEqual("cal", table[2][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFollowingOnlyFramedValueWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'amy' as Name, 1 as Score from #system.dual()
                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()
                )
                select FirstValue(c.Name) over (order by c.Score rows between 1 following and 2 following) as NextName
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeFirstValueWindow [resultFirstValues <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 following and 2 following]", result.ExecutionPlanText);
        Assert.Contains("resultFirstValuesFrameStart", result.GeneratedCSharpCode);
        Assert.Contains("resultFirstValuesSourcePartitionIndex", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFollowingOnlyFramedValueWindow_ShouldRunExecutableQuery()
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
                       FirstValue(c.Name) over (order by c.Score rows between 1 following and 2 following) as FirstNextName,
                       LastValue(c.Name) over (order by c.Score rows between 1 following and 2 following) as LastNextName
                from c
                order by c.Score",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("bea", table[0][2]);
        Assert.AreEqual("cal", table[0][3]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("cal", table[1][2]);
        Assert.AreEqual("dan", table[1][3]);
        Assert.AreEqual("cal", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual("dan", table[2][2]);
        Assert.AreEqual("dan", table[2][3]);
        Assert.AreEqual("dan", table[3][0]);
        Assert.AreEqual(4, table[3][1]);
        Assert.IsNull(table[3][2]);
        Assert.IsNull(table[3][3]);
    }

}
