using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForMultipleWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       RowNumber() over (order by c.Score) as Rn,\n"
                + "                       Lag(c.Score, 1) over (order by c.Score) as PrevScore\n"
                + "                from c"
            ),
            new CompilationOptions()
        );

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains(
            "ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows order by c.Score ASC]",
            result.ExecutionPlanText
        );
        Assert.Contains(
            "ComputeLagWindow [resultLags1 <- resultWindowRows value c.Score order by c.Score ASC frame range between unbounded preceding and current row args 1, NULL]",
            result.ExecutionPlanText
        );
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForMultipleWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       RowNumber() over (order by c.Score) as Rn,\n"
                + "                       Lag(c.Score, 1) over (order by c.Score) as PrevScore\n"
                + "                from c\n"
                + "                order by Rn"
            ),
            new CompilationOptions()
        );

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0].ReadValue(0));
        Assert.AreEqual(1L, table[0].ReadValue(1));
        Assert.IsNull(table[0].ReadValue(2));
        Assert.AreEqual("bea", table[1].ReadValue(0));
        Assert.AreEqual(2L, table[1].ReadValue(1));
        Assert.AreEqual(1, table[1].ReadValue(2));
        Assert.AreEqual("cal", table[2].ReadValue(0));
        Assert.AreEqual(3L, table[2].ReadValue(1));
        Assert.AreEqual(2, table[2].ReadValue(2));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForValueWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       FirstValue(c.Name) over (order by c.Score) as FirstName,\n"
                + "                       NthValue(c.Name, 2) over (order by c.Score) as SecondName\n"
                + "                from c"
            ),
            new CompilationOptions()
        );

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains(
            "ComputeFirstValueWindow [resultFirstValues0 <- resultWindowRows value c.Name order by c.Score ASC frame range between unbounded preceding and current row]",
            result.ExecutionPlanText
        );
        Assert.Contains(
            "ComputeNthValueWindow [resultNthValues1 <- resultWindowRows value c.Name order by c.Score ASC frame range between unbounded preceding and current row args 2]",
            result.ExecutionPlanText
        );
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForValueWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       c.Score,\n"
                + "                       FirstValue(c.Name) over (order by c.Score) as FirstName,\n"
                + "                       LastValue(c.Name) over (order by c.Score) as LastName,\n"
                + "                       NthValue(c.Name, 2) over (order by c.Score) as SecondName\n"
                + "                from c\n"
                + "                order by c.Score"
            ),
            new CompilationOptions()
        );

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual("amy", table[0].ReadValue(2));
        Assert.AreEqual("amy", table[0].ReadValue(3));
        Assert.IsNull(table[0].ReadValue(4));
        Assert.AreEqual("bea", table[1].ReadValue(0));
        Assert.AreEqual(2, table[1].ReadValue(1));
        Assert.AreEqual("amy", table[1].ReadValue(2));
        Assert.AreEqual("bea", table[1].ReadValue(3));
        Assert.AreEqual("bea", table[1].ReadValue(4));
        Assert.AreEqual("cal", table[2].ReadValue(0));
        Assert.AreEqual(3, table[2].ReadValue(1));
        Assert.AreEqual("amy", table[2].ReadValue(2));
        Assert.AreEqual("cal", table[2].ReadValue(3));
        Assert.AreEqual("bea", table[2].ReadValue(4));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRowDependentValueWindowArguments_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score, 1 as Position from #system.dual()\n"
                + "                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 2 as Position from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       NthValue(c.Name, c.Position) over (order by c.Score) as PickedName\n"
                + "                from c"
            ),
            new CompilationOptions()
        );

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains(
            "ComputeNthValueWindow [resultNthValues <- resultWindowRows value c.Name order by c.Score ASC frame range between unbounded preceding and current row args c.Position]",
            result.ExecutionPlanText
        );
        Assert.Contains("resultNthValuesArgument0", result.GeneratedCSharpCode);
        Assert.Contains("NthReferenceValueKernel<string>.Get(in resultNthValuesContext", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputePluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRowDependentValueWindowArguments_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score, 1 as Position from #system.dual()\n"
                + "                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 2 as Position from #system.dual()\n"
                + "                    union all (Name, Score, Position) select 'cal' as Name, 3 as Score, 1 as Position from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       c.Score,\n"
                + "                       NthValue(c.Name, c.Position) over (order by c.Score) as PickedName\n"
                + "                from c\n"
                + "                order by c.Score"
            ),
            new CompilationOptions()
        );

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual("amy", table[0].ReadValue(2));
        Assert.AreEqual("bea", table[1].ReadValue(0));
        Assert.AreEqual(2, table[1].ReadValue(1));
        Assert.AreEqual("bea", table[1].ReadValue(2));
        Assert.AreEqual("cal", table[2].ReadValue(0));
        Assert.AreEqual(3, table[2].ReadValue(1));
        Assert.AreEqual("amy", table[2].ReadValue(2));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedValueWindows_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       FirstValue(c.Name) over (order by c.Score rows between 1 preceding and 1 following) as FirstName,\n"
                + "                       NthValue(c.Name, 2) over (order by c.Score rows between 1 preceding and 1 following) as SecondName\n"
                + "                from c"
            ),
            new CompilationOptions()
        );

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains(
            "ComputeFirstValueWindow [resultFirstValues0 <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 preceding and 1 following]",
            result.ExecutionPlanText
        );
        Assert.Contains(
            "ComputeNthValueWindow [resultNthValues1 <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 preceding and 1 following args 2]",
            result.ExecutionPlanText
        );
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedValueWindows_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'dan' as Name, 4 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       c.Score,\n"
                + "                       FirstValue(c.Name) over (order by c.Score rows between 1 preceding and 1 following) as FirstName,\n"
                + "                       LastValue(c.Name) over (order by c.Score rows between 1 preceding and 1 following) as LastName,\n"
                + "                       NthValue(c.Name, 2) over (order by c.Score rows between 1 preceding and 1 following) as SecondName\n"
                + "                from c\n"
                + "                order by c.Score"
            ),
            new CompilationOptions()
        );

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual("amy", table[0].ReadValue(2));
        Assert.AreEqual("bea", table[0].ReadValue(3));
        Assert.AreEqual("bea", table[0].ReadValue(4));
        Assert.AreEqual("bea", table[1].ReadValue(0));
        Assert.AreEqual(2, table[1].ReadValue(1));
        Assert.AreEqual("amy", table[1].ReadValue(2));
        Assert.AreEqual("cal", table[1].ReadValue(3));
        Assert.AreEqual("bea", table[1].ReadValue(4));
        Assert.AreEqual("cal", table[2].ReadValue(0));
        Assert.AreEqual(3, table[2].ReadValue(1));
        Assert.AreEqual("bea", table[2].ReadValue(2));
        Assert.AreEqual("dan", table[2].ReadValue(3));
        Assert.AreEqual("cal", table[2].ReadValue(4));
        Assert.AreEqual("dan", table[3].ReadValue(0));
        Assert.AreEqual(4, table[3].ReadValue(1));
        Assert.AreEqual("cal", table[3].ReadValue(2));
        Assert.AreEqual("dan", table[3].ReadValue(3));
        Assert.AreEqual("dan", table[3].ReadValue(4));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFramedRowDependentValueWindowArguments_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score, 2 as Position from #system.dual()\n"
                + "                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 3 as Position from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       NthValue(c.Name, c.Position) over (order by c.Score rows between 1 preceding and 1 following) as PickedName\n"
                + "                from c"
            ),
            new CompilationOptions()
        );

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains(
            "ComputeNthValueWindow [resultNthValues <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 preceding and 1 following args c.Position]",
            result.ExecutionPlanText
        );
        Assert.Contains("resultNthValuesArgument0", result.GeneratedCSharpCode);
        Assert.Contains("resultNthValuesFrameStart", result.GeneratedCSharpCode);
        Assert.Contains("NthReferenceValueKernel<string>.Get(in resultNthValuesContext", result.GeneratedCSharpCode);
        Assert.IsFalse(
            result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal)
        );
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFramedRowDependentValueWindowArguments_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score, 2 as Position from #system.dual()\n"
                + "                    union all (Name, Score, Position) select 'bea' as Name, 2 as Score, 3 as Position from #system.dual()\n"
                + "                    union all (Name, Score, Position) select 'cal' as Name, 3 as Score, 1 as Position from #system.dual()\n"
                + "                    union all (Name, Score, Position) select 'dan' as Name, 4 as Score, 2 as Position from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       c.Score,\n"
                + "                       NthValue(c.Name, c.Position) over (order by c.Score rows between 1 preceding and 1 following) as PickedName\n"
                + "                from c\n"
                + "                order by c.Score"
            ),
            new CompilationOptions()
        );

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual("bea", table[0].ReadValue(2));
        Assert.AreEqual("bea", table[1].ReadValue(0));
        Assert.AreEqual(2, table[1].ReadValue(1));
        Assert.AreEqual("cal", table[1].ReadValue(2));
        Assert.AreEqual("cal", table[2].ReadValue(0));
        Assert.AreEqual(3, table[2].ReadValue(1));
        Assert.AreEqual("bea", table[2].ReadValue(2));
        Assert.AreEqual("dan", table[3].ReadValue(0));
        Assert.AreEqual(4, table[3].ReadValue(1));
        Assert.AreEqual("dan", table[3].ReadValue(2));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForRangeFramedValueWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                )\n"
                + "                select FirstValue(c.Name) over (order by c.Score range between 1 preceding and 1 following) as FirstName\n"
                + "                from c"
            ),
            new CompilationOptions()
        );

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains(
            "ComputeFirstValueWindow [resultFirstValues <- resultWindowRows value c.Name order by c.Score ASC frame range between 1 preceding and 1 following]",
            result.ExecutionPlanText
        );
        Assert.Contains("resultFirstValuesFrameStart", result.GeneratedCSharpCode);
        Assert.IsFalse(
            result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal)
        );
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForRangeFramedValueWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                       c.Score,\n"
                + "                       FirstValue(c.Name) over (order by c.Score range between 1 preceding and 1 following) as FirstName,\n"
                + "                       LastValue(c.Name) over (order by c.Score range between current row and unbounded following) as LastName\n"
                + "                from c\n"
                + "                order by c.Score"
            ),
            new CompilationOptions()
        );

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("amy", table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual("amy", table[0].ReadValue(2));
        Assert.AreEqual("cal", table[0].ReadValue(3));
        Assert.AreEqual("bea", table[1].ReadValue(0));
        Assert.AreEqual(2, table[1].ReadValue(1));
        Assert.AreEqual("amy", table[1].ReadValue(2));
        Assert.AreEqual("cal", table[1].ReadValue(3));
        Assert.AreEqual("cal", table[2].ReadValue(0));
        Assert.AreEqual(3, table[2].ReadValue(1));
        Assert.AreEqual("bea", table[2].ReadValue(2));
        Assert.AreEqual("cal", table[2].ReadValue(3));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForFollowingOnlyFramedValueWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                )\n"
                + "                select FirstValue(c.Name) over (order by c.Score rows between 1 following and 2 following) as NextName\n"
                + "                from c"
            ),
            new CompilationOptions()
        );

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains(
            "ComputeFirstValueWindow [resultFirstValues <- resultWindowRows value c.Name order by c.Score ASC frame rows between 1 following and 2 following]",
            result.ExecutionPlanText
        );
        Assert.Contains("resultFirstValuesFrameStart", result.GeneratedCSharpCode);
        Assert.Contains(
            "FirstReferenceValueKernel<string>.Get(in resultFirstValuesContext)",
            result.GeneratedCSharpCode
        );
        Assert.IsFalse(
            result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal)
        );
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForFollowingOnlyFramedValueWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            (
                "\n"
                + "                with c as (\n"
                + "                    select 'amy' as Name, 1 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'bea' as Name, 2 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'cal' as Name, 3 as Score from #system.dual()\n"
                + "                    union all (Name, Score) select 'dan' as Name, 4 as Score from #system.dual()\n"
                + "                )\n"
                + "                select c.Name,\n"
                + "                      c.Score,\n"
                + "                       FirstValue(c.Name) over (order by c.Score rows between 1 following and 2 following) as FirstNextName,\n"
                + "                       LastValue(c.Name) over (order by c.Score rows between 1 following and 2 following) as LastNextName\n"
                + "                from c\n"
                + "                order by c.Score"
            ),
            new CompilationOptions()
        );

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("amy", table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual("bea", table[0].ReadValue(2));
        Assert.AreEqual("cal", table[0].ReadValue(3));
        Assert.AreEqual("bea", table[1].ReadValue(0));
        Assert.AreEqual(2, table[1].ReadValue(1));
        Assert.AreEqual("cal", table[1].ReadValue(2));
        Assert.AreEqual("dan", table[1].ReadValue(3));
        Assert.AreEqual("cal", table[2].ReadValue(0));
        Assert.AreEqual(3, table[2].ReadValue(1));
        Assert.AreEqual("dan", table[2].ReadValue(2));
        Assert.AreEqual("dan", table[2].ReadValue(3));
        Assert.AreEqual("dan", table[3].ReadValue(0));
        Assert.AreEqual(4, table[3].ReadValue(1));
        Assert.IsNull(table[3].ReadValue(2));
        Assert.IsNull(table[3].ReadValue(3));
    }
}
