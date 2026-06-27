using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderQualifiedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect(
            "select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m qualify RowNumber() over (partition by i.Name order by n.Value, m.Value) <= 1 order by i.Name",
            CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        Assert.Contains("CreateRowBuffer [apply_0_i_n_mTable: List<apply_0_i_n_mRow0>]", result.ExecutionPlanText);
        Assert.Contains("Materialize [apply_0_i_n_mTable -> resultWindowRows]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateTable [apply_0_i_n_mTable: apply_0_i_n_mRow0]", StringComparison.Ordinal));
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        Assert.Contains("qualify <= 1", result.ExecutionPlanText);
        Assert.Contains("If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 1))]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererDisabledForChainedCrossApplyPropertySource_ShouldStillUseExecutionBackend()
    {
        var result = Inspect("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m",
            CreateApplyCandidateSchemaProvider(),
            new CompilationOptions());

        Assert.Contains("ExecutionPlan [compiled]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCteWrappedChainedApplyWithDuplicateColumns_ShouldUseExecutionBackend()
    {
        var result = Inspect(
            @"
                with expanded as (
                    select i.Name as Name, n.Value as Value, m.Value as Value
                    from #apply.items() i
                    cross apply i.Numbers n
                    cross apply i.Numbers m
                )
                select * from expanded e", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.Contains("StoreTable [cte0_statement0 -> _cteRowResults.Slot1", result.ExecutionPlanText);
        Assert.Contains("TableRow [e]", result.ExecutionPlanText);
        Assert.Contains("n.Value: int <- field n_Value", result.ExecutionPlanText);
        Assert.Contains("m.Value: int <- field m_Value", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

}
