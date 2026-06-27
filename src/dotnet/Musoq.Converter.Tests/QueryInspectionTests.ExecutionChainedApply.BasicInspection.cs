using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name, n.Value from #apply.items() i cross apply i.Numbers n", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("EnumerableSource [i.Numbers -> nRows]", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [n in nRows]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name, n.Value from #apply.items() i cross apply i.Numbers n", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("right", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderFilteredChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m where n.Value < m.Value", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        Assert.Contains("Let [value: int = n.Value]", result.ExecutionPlanText);
        Assert.Contains("Let [value1: int = m.Value]", result.ExecutionPlanText);
        Assert.Contains("If [(value < value1)]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderSortedPaginatedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by FirstValue, SecondValue skip 1 take 2", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        Assert.Contains("TopOffsetShapeRows [result -> resultTopOffset by FirstValue ASC, SecondValue ASC, skip 1, take 2, BoundedHeap]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("SortShapeRows [result -> resultSorted", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("SliceTable [resultSorted -> resultSortedSliced", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderNonProjectedChainedCrossApplyOrdering_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by n.Value desc, m.Value take 3", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.Contains("Generated [ResultRow0WithSortKeys]", result.ExecutionPlanText);
        Assert.Contains("__sortKey0: int <- field __sortKey0", result.ExecutionPlanText);
        Assert.Contains("__sortKey1: int <- field __sortKey1", result.ExecutionPlanText);
        Assert.Contains("TopNTable [resultWithSortKeys -> resultWithSortKeysTopN by __sortKey0 DESC, __sortKey1 ASC, 3]", result.ExecutionPlanText);
        Assert.Contains("ProjectShapeRows [resultWithSortKeysTopN -> result fields 0]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

}
