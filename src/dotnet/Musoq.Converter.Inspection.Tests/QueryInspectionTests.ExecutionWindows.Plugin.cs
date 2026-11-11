using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForCustomPluginWindow_ShouldEmitExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'Charlie' as Name, 'NYC' as City, 5 as Population from #system.dual()
                    union all (Name, City, Population) select 'Alice' as Name, 'LA' as City, 2 as Population from #system.dual()
                    union all (Name, City, Population) select 'Bob' as Name, 'NYC' as City, 3 as Population from #system.dual()
                )
                select c.Name,
                       RunningProduct(c.Population) over (partition by c.City order by c.Name) as Product
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRunningProductWindowKernel[BoundedRows] [", result.ExecutionPlanText);
        Assert.Contains("value c.Population partition by c.City order by c.Name ASC", result.ExecutionPlanText);
        Assert.Contains("RunningProductKernel.Set(ref ", result.GeneratedCSharpCode);
        Assert.Contains("RunningProductKernel.State", result.GeneratedCSharpCode);
        Assert.Contains("Kernel.Reset(ref ", result.GeneratedCSharpCode);
        Assert.Contains("Kernel.Get(in ", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".AccumulateValue(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetCurrentValue(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("SetArguments(Array.Empty<object?>())", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("RunningProductValues", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("RunningProductArguments", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeTypedPluginWindowFunction<object, decimal>", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputePluginWindowFunction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForCustomRunningFramedPluginWindow_ShouldStreamExecutionIrWindowPlan()
    {
        var result = Inspect(@"
                with c as (
                    select 'Charlie' as Name, 'NYC' as City, 5 as Population from #system.dual()
                    union all (Name, City, Population) select 'Alice' as Name, 'LA' as City, 2 as Population from #system.dual()
                    union all (Name, City, Population) select 'Bob' as Name, 'NYC' as City, 3 as Population from #system.dual()
                )
                select c.Name,
                       RunningProduct(c.Population) over (
                           partition by c.City
                           order by c.Name
                           rows between unbounded preceding and current row) as Product
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ComputeRunningProductWindowKernel[Running] [", result.ExecutionPlanText);
        Assert.Contains("frame rows between unbounded preceding and current row", result.ExecutionPlanText);
        Assert.Contains("RunningProductKernel.Set(ref ", result.GeneratedCSharpCode);
        Assert.Contains("RunningProductKernel.State", result.GeneratedCSharpCode);
        Assert.Contains("Kernel.Reset(ref ", result.GeneratedCSharpCode);
        Assert.Contains("Kernel.Get(in ", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".AccumulateValue(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetCurrentValue(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("SetArguments(Array.Empty<object?>())", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("RunningProductValues", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("RunningProductArguments", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ComputeTypedPluginWindowFunction<object, decimal>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForCustomPluginWindowArguments_ShouldEmitTypedArgumentDispatch()
    {
        var result = Inspect(@"
                with c as (
                    select 'Charlie' as Name, 'NYC' as City, 5 as Population from #system.dual()
                    union all (Name, City, Population) select 'Alice' as Name, 'LA' as City, 2 as Population from #system.dual()
                    union all (Name, City, Population) select 'Bob' as Name, 'NYC' as City, 3 as Population from #system.dual()
                )
                select c.Name,
                       ScaledRunningProduct(c.Population, 2) over (partition by c.City order by c.Name) as Product
                from c",
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("ScaledRunningProductKernel.Set(ref ", result.GeneratedCSharpCode);
        Assert.Contains("RunningProductKernel.State", result.GeneratedCSharpCode);
        Assert.Contains("ScaledRunningProductKernel.State", result.GeneratedCSharpCode);
        Assert.Contains(", (int)2);", result.GeneratedCSharpCode);
        Assert.Contains("Kernel.Reset(ref ", result.GeneratedCSharpCode);
        Assert.Contains("Kernel.Get(in ", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("ScaledRunningProductArguments", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("SetArguments(new object", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("SetArguments(object?[]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".AccumulateValue(", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetCurrentValue(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererUsesCustomPluginWindowArguments_ShouldRunTypedArgumentDispatch()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'Charlie' as Name, 'NYC' as City, 5 as Population from #system.dual()
                    union all (Name, City, Population) select 'Alice' as Name, 'LA' as City, 2 as Population from #system.dual()
                    union all (Name, City, Population) select 'Bob' as Name, 'NYC' as City, 3 as Population from #system.dual()
                    union all (Name, City, Population) select 'Diana' as Name, 'LA' as City, 4 as Population from #system.dual()
                )
                select c.Name,
                       c.City,
                       ScaledRunningProduct(c.Population, 2) over (partition by c.City order by c.Name) as Product
                from c
                order by c.City, c.Name",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValue(0));
        Assert.AreEqual("LA", table[0].ReadValue(1));
        Assert.AreEqual(4m, table[0].ReadValue(2));
        Assert.AreEqual("Diana", table[1].ReadValue(0));
        Assert.AreEqual("LA", table[1].ReadValue(1));
        Assert.AreEqual(32m, table[1].ReadValue(2));
        Assert.AreEqual("Bob", table[2].ReadValue(0));
        Assert.AreEqual("NYC", table[2].ReadValue(1));
        Assert.AreEqual(6m, table[2].ReadValue(2));
        Assert.AreEqual("Charlie", table[3].ReadValue(0));
        Assert.AreEqual("NYC", table[3].ReadValue(1));
        Assert.AreEqual(60m, table[3].ReadValue(2));
    }

    [TestMethod]
    public void CompileForInspection_WhenCustomPluginWindowIsObjectShaped_ShouldRejectNoBoxingExecution()
    {
        var exception = Assert.Throws<global::Musoq.Evaluator.Exceptions.SourcePlanContractException>(() => Inspect(@"
                with c as (
                    select 'Charlie' as Name, 'NYC' as City, 5 as Population from #system.dual()
                )
                select ObjectRunningProduct(c.Population) over (partition by c.City order by c.Name) as Product
                from c",
            new CompilationOptions()));

        var envelope = MusoqErrorEnvelope.FromException(exception);
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, envelope.Code);
        Assert.Contains("ObjectRunningProduct", exception.Message);
        var verbose = MusoqErrorEnvelope.FromExceptionVerbose(exception);
        Assert.Contains("ObjectRunningProduct", verbose.Details ?? string.Empty);
        Assert.Contains("erased or boxed argument", verbose.Details ?? string.Empty);
        Assert.Contains("concrete typed overload", verbose.Details ?? string.Empty);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForCustomPluginWindow_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                with c as (
                    select 'Charlie' as Name, 'NYC' as City, 5 as Population from #system.dual()
                    union all (Name, City, Population) select 'Alice' as Name, 'LA' as City, 2 as Population from #system.dual()
                    union all (Name, City, Population) select 'Bob' as Name, 'NYC' as City, 3 as Population from #system.dual()
                    union all (Name, City, Population) select 'Diana' as Name, 'LA' as City, 4 as Population from #system.dual()
                )
                select c.Name,
                       c.City,
                       RunningProduct(c.Population) over (partition by c.City order by c.Name) as Product,
                       Sum(c.Population) over (partition by c.City order by c.Name) as RunningSum
                from c
                order by c.City, c.Name",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValue(0));
        Assert.AreEqual("LA", table[0].ReadValue(1));
        Assert.AreEqual(2m, table[0].ReadValue(2));
        Assert.AreEqual(2, table[0].ReadValue(3));
        Assert.AreEqual("Diana", table[1].ReadValue(0));
        Assert.AreEqual("LA", table[1].ReadValue(1));
        Assert.AreEqual(8m, table[1].ReadValue(2));
        Assert.AreEqual(6, table[1].ReadValue(3));
        Assert.AreEqual("Bob", table[2].ReadValue(0));
        Assert.AreEqual("NYC", table[2].ReadValue(1));
        Assert.AreEqual(3m, table[2].ReadValue(2));
        Assert.AreEqual(3, table[2].ReadValue(3));
        Assert.AreEqual("Charlie", table[3].ReadValue(0));
        Assert.AreEqual("NYC", table[3].ReadValue(1));
        Assert.AreEqual(15m, table[3].ReadValue(2));
        Assert.AreEqual(8, table[3].ReadValue(3));
    }

}
