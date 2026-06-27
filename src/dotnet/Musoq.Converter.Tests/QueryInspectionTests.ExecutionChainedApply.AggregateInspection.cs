using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name, n.Value as FirstValue, m.Value as SecondValue, RowNumber() over (partition by i.Name order by n.Value, m.Value) as RowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by i.Name, RowNo", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        Assert.Contains("CreateRowBuffer [apply_0_i_n_mTable: List<apply_0_i_n_mRow0>]", result.ExecutionPlanText);
        Assert.Contains("Materialize [apply_0_i_n_mTable -> resultWindowRows]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateTable [apply_0_i_n_mTable: apply_0_i_n_mRow0]", StringComparison.Ordinal));
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Count(1) as PairCount from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedHavingChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Count(1) as PairCount from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Count(1) > 1 order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("If [(Count('Count(1)') > 1)]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Count(1) as PairCount, RowNumber() over (order by i.Name) as GroupRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by GroupRowNo", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedQualifiedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Count(1) as PairCount from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name qualify RowNumber() over (order by i.Name) <= 1 order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        Assert.Contains("qualify <= 1", result.ExecutionPlanText);
        Assert.Contains("If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 1))]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedSumWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Sum(n.Value) as ValueSum, RowNumber() over (order by Sum(n.Value) desc) as GroupRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by GroupRowNo", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedHavingQualifiedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Sum(n.Value) as ValueSum from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Sum(n.Value) > 2 qualify RowNumber() over (order by Sum(n.Value) desc) <= 1 order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        Assert.Contains("qualify <= 1", result.ExecutionPlanText);
        Assert.Contains("If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 1))]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderPartitionedGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, RowNumber() over (partition by Count(1) order by Avg(n.Value) desc, Min(n.Value), Max(n.Value) desc) as AggregateRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        Assert.Contains("partition by windowSource.Count(1)", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedAvgMinMaxHavingQualifiedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Avg(n.Value) as ValueAvg, Min(n.Value) as ValueMin, Max(n.Value) as ValueMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Max(n.Value) >= 2 qualify RowNumber() over (order by Avg(n.Value) desc, Min(n.Value), Max(n.Value) desc) <= 1 order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertChainedApplyStreamsWithoutFirstTransition(result.ExecutionPlanText);
        AssertChainedApplyDoesNotMaterializeFinalApplyTable(result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        Assert.Contains("qualify <= 1", result.ExecutionPlanText);
        Assert.Contains("If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 1))]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderFilteredGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, RowNumber() over (order by Sum(n.Value) filter (where m.Value > 1) desc, i.Name) as FilteredSumRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("CASE WHEN", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMultiArgumentGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, RowNumber() over (order by Sum(n.Value, 0) desc, i.Name) as ParentSumRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("Sum(Value,0): int?", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAliasDistinctGroupedAggregateChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Sum(n.Value) as NumberTotal, Sum(b.Value) as ByteTotal from #apply.items() i cross apply i.Numbers n cross apply i.Content b group by i.Name order by Name", CreateAliasDistinctAggregateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAliasDistinctGroupedAggregateSortChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Sum(n.Value) as NumberTotal, Sum(b.Value) as ByteTotal from #apply.items() i cross apply i.Numbers n cross apply i.Content b group by i.Name order by Sum(b.Value) desc, Sum(n.Value) desc", CreateAliasDistinctAggregateSortSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("SortShapeRows [result -> resultSorted by ByteTotal DESC, NumberTotal DESC]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAliasDistinctGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Sum(n.Value) as NumberTotal, Sum(b.Value) as ByteTotal, RowNumber() over (order by Sum(b.Value) desc, Sum(n.Value) desc, i.Name) as AggregateRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Content b group by i.Name order by AggregateRowNo", CreateAliasDistinctAggregateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderDistinctMinMaxGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, RowNumber() over (order by Max(distinct n.Value) desc, Min(distinct n.Value), i.Name) as DistinctMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetDistinctAggregate(", StringComparison.Ordinal));
        Assert.Contains("MaxDistinct(", result.ExecutionPlanText);
        Assert.Contains("MinDistinct(", result.ExecutionPlanText);
        Assert.Contains("MaxDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("MinDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Sum(n.Value) as RepeatedSum, Sum(distinct n.Value) as DistinctSum, RowNumber() over (order by Sum(distinct n.Value) desc, Sum(n.Value) desc, i.Name) as MixedRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedRowNo", CreateMixedRegularAndDistinctAggregateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetDistinctAggregate(", StringComparison.Ordinal));
        Assert.Contains("SumDistinct(", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctGroupedAggregateSortChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Sum(n.Value) as RepeatedSum, Sum(distinct n.Value) as DistinctSum from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Sum(distinct n.Value) desc, Sum(n.Value) desc, i.Name", CreateMixedRegularAndDistinctAggregateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetDistinctAggregate(", StringComparison.Ordinal));
        Assert.Contains("SumDistinct(", result.ExecutionPlanText);
        Assert.Contains("SortShapeRows [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctMinMaxGroupedAggregateSortChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name", CreateMixedDistinctAggregateFamilySchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetDistinctAggregate(", StringComparison.Ordinal));
        Assert.Contains("MinDistinct(", result.ExecutionPlanText);
        Assert.Contains("MaxDistinct(", result.ExecutionPlanText);
        Assert.Contains("MinDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("MaxDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("SortShapeRows [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ComputeRowNumberWindow [", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctAvgGroupedAggregateSortChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name", CreateMixedDistinctAggregateFamilySchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetDistinctAggregate(", StringComparison.Ordinal));
        Assert.Contains("AvgDistinct(", result.ExecutionPlanText);
        Assert.Contains("AvgDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("SortShapeRows [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ComputeRowNumberWindow [", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctMinMaxGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax, RowNumber() over (order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name) as MixedMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedMinMaxRowNo", CreateMixedDistinctAggregateFamilySchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetDistinctAggregate(", StringComparison.Ordinal));
        Assert.Contains("MinDistinct(", result.ExecutionPlanText);
        Assert.Contains("MaxDistinct(", result.ExecutionPlanText);
        Assert.Contains("MinDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("MaxDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMixedRegularAndDistinctAvgGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg, RowNumber() over (order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name) as MixedAvgRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedAvgRowNo", CreateMixedDistinctAggregateFamilySchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".SetDistinctAggregate(", StringComparison.Ordinal));
        Assert.Contains("AvgDistinct(", result.ExecutionPlanText);
        Assert.Contains("AvgDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAvgParentGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, RowNumber() over (order by Avg(n.Value, 0) desc, i.Name) as ParentAvgRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("Avg(Value,0): int?", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderMinMaxParentGroupedAggregateWindowedChainedCrossApplyPropertySource_ShouldUseExecutionBackend()
    {
        var result = Inspect("select i.Name as Name, RowNumber() over (order by Max(n.Value, 0) desc, Min(n.Value, 0), i.Name) as ParentMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Name", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("Max(Value,0): int?", result.ExecutionPlanText);
        Assert.Contains("Min(Value,0): int?", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

}
