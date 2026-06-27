using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForAggregateOnly_ShouldEmitExecutionAggregateCode()
    {
        var result = Inspect(
            "select Count(1) as Count from #system.dual() d",
            new CompilationOptions());

        AssertTypedAggregateContext(result.ExecutionPlanText);
        Assert.Contains("Count(", result.ExecutionPlanText);
        Assert.Contains("private sealed class ResultAggregateGroup", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(
            "private sealed class ResultAggregateGroup : Group",
            StringComparison.Ordinal));
        AssertGeneratedCSharpContains("group.__agg0.Count = checked(group.__agg0.Count + 1L)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__agg0.Count", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForCountDistinct_ShouldEmitTypedKernel()
    {
        var result = Inspect(
            "select Count(distinct d.Dummy) as DistinctCount from #system.dual() d",
            new CompilationOptions());

        AssertTypedAggregateContext(result.ExecutionPlanText);
        Assert.Contains("CountDistinct(", result.ExecutionPlanText);
        AssertGeneratedCSharpContains(
            "CountDistinctReferenceAggregateKernel<string>.Set(ref group.__agg0",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "CountDistinctReferenceAggregateKernel<string>.Get(in finalGroup.__agg0)",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("HashSet<object>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForStDevAggregate_ShouldEmitTypedKernel()
    {
        var result = Inspect(
            """
            with values as (
                select 10 as Population from #system.dual()
                union all (Population) select 20 as Population from #system.dual()
            )
            select StDev(Population) as PopulationStDev from values
            """,
            new CompilationOptions());

        AssertTypedAggregateContext(result.ExecutionPlanText);
        Assert.Contains("TypedAggregateSet [", result.ExecutionPlanText);
        AssertGeneratedCSharpContains(
            "StDevAggregateKernel<int>.Set(ref group.__agg0",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "StDevAggregateKernel<int>.Get(in finalGroup.__agg0)",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetValue<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForIncomeOutcomeAggregates_ShouldEmitTypedKernels()
    {
        var result = Inspect(
            """
            with amounts as (
                select 10 as Amount from #system.dual()
                union all (Amount) select -3 as Amount from #system.dual()
            )
            select SumIncome(Amount) as Income, SumOutcome(Amount) as Outcome from amounts
            """,
            new CompilationOptions());

        AssertTypedAggregateContext(result.ExecutionPlanText);
        Assert.Contains("TypedAggregateSet [", result.ExecutionPlanText);
        Assert.Contains("SumIncomeAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("SumOutcomeAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("SumIncomeAggregateKernel<int>.Get", result.GeneratedCSharpCode);
        Assert.Contains("SumOutcomeAggregateKernel<int>.Get", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetValue<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForParentAggregates_ShouldEmitTypedParentAggregateCode()
    {
        var result = Inspect(
            """
            with amounts as (
                select 'PL' as Country, 'Warsaw' as City, 10 as Amount from #system.dual()
                union all (Country, City, Amount) select 'PL' as Country, 'Krakow' as City, -3 as Amount from #system.dual()
            )
            select
                Country,
                City,
                Count(City, 1) as CountryRows,
                SumIncome(Amount, 1) as CountryIncome,
                SumOutcome(Amount, 1) as CountryOutcome,
                Count(City) as CityRows
            from amounts
            group by Country, City
            """,
            new CompilationOptions());

        AssertTypedValueTupleAggregateContext(result.ExecutionPlanText);
        Assert.Contains("AggregateGroup [", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("private sealed class ResultAggregateGroupPrefix1", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("private sealed class ResultAggregateGroup", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("public readonly ResultAggregateGroupPrefix1 __owner1", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groupsLevel_0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(".Count = checked(group.__owner1.__agg", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("SumIncomeAggregateKernel<int>.Set(ref group.__owner1.__agg", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("SumOutcomeAggregateKernel<int>.Set(ref group.__owner1.__agg", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__owner1.__agg", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("SumIncomeAggregateKernel<int>.Get(in finalGroup.__owner1.__agg", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("SumOutcomeAggregateKernel<int>.Get(in finalGroup.__owner1.__agg", result.GeneratedCSharpCode);
        AssertNoLegacyAggregateRuntime(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForParentAggregates_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            """
            with amounts as (
                select 'PL' as Country, 'Warsaw' as City, 10 as Amount from #system.dual()
                union all (Country, City, Amount) select 'PL' as Country, 'Krakow' as City, -3 as Amount from #system.dual()
            )
            select
                Country,
                City,
                Count(City, 1) as CountryRows,
                SumIncome(Amount, 1) as CountryIncome,
                SumOutcome(Amount, 1) as CountryOutcome,
                Count(City) as CityRows
            from amounts
            group by Country, City
            """,
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        foreach (var row in table)
        {
            Assert.AreEqual("PL", row[0]);
            Assert.AreEqual(2L, row[2]);
            Assert.AreEqual(10, row[3]);
            Assert.AreEqual(-3, row[4]);
            Assert.AreEqual(1L, row[5]);
        }
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForCustomAggregate_ShouldEmitTypedKernel()
    {
        var result = Inspect(
            "select d.Dummy as Dummy, CustomLengthTotal(Length(d.Dummy)) as TotalLength from #system.dual() d group by d.Dummy",
            new CompilationOptions());

        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        AssertGeneratedCSharpContains("CustomLengthTotalAggregate.Set(ref group.__agg0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("CustomLengthTotalAggregate.Get(in finalGroup.__agg0", result.GeneratedCSharpCode);
        AssertNoLegacyAggregateRuntime(result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForTimeSpanAggregates_ShouldEmitTypedKernels()
    {
        var result = Inspect(
            """
            with periods as (
                select ToTimeSpan('01:00:00') as Period from #system.dual()
                union all (Period) select ToTimeSpan('02:00:00') as Period from #system.dual()
            )
            select SumTimeSpan(Period) as Total, MinTimeSpan(Period) as Minimum, MaxTimeSpan(Period) as Maximum from periods
            """,
            new CompilationOptions());

        AssertTypedAggregateContext(result.ExecutionPlanText);
        Assert.Contains("TypedAggregateSet [", result.ExecutionPlanText);
        AssertGeneratedCSharpContains(
            "SumTimeSpanAggregateKernel.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "MinComparableAggregateKernel<TimeSpan>.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "MaxComparableAggregateKernel<TimeSpan>.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "SumTimeSpanAggregateKernel.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "MinComparableAggregateKernel<TimeSpan>.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "MaxComparableAggregateKernel<TimeSpan>.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetValue<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForDateAggregates_ShouldEmitTypedKernels()
    {
        var result = Inspect(
            """
            with dates as (
                select ToDateTime('2020/01/01') as DateValue, ToDateTimeOffset('2020-01-01T00:00:00+00:00') as OffsetValue from #system.dual()
                union all (DateValue, OffsetValue) select ToDateTime('2021/01/01') as DateValue, ToDateTimeOffset('2021-01-01T00:00:00+00:00') as OffsetValue from #system.dual()
            )
            select MinDateTime(DateValue) as EarliestDate, MaxDateTimeOffset(OffsetValue) as LatestOffset from dates
            """,
            new CompilationOptions());

        AssertTypedAggregateContext(result.ExecutionPlanText);
        Assert.Contains("TypedAggregateSet [", result.ExecutionPlanText);
        AssertGeneratedCSharpContains(
            "MinComparableAggregateKernel<DateTime>.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "MaxComparableAggregateKernel<DateTimeOffset>.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "MinComparableAggregateKernel<DateTime>.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "MaxComparableAggregateKernel<DateTimeOffset>.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetValue<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForStringAggregateValues_ShouldEmitTypedKernels()
    {
        var result = Inspect(
            """
            with people as (
                select 'Alice' as Name from #system.dual()
                union all (Name) select 'Bob' as Name from #system.dual()
            )
            select AggregateValues(Name) as Names, AggregateValues(Name, ', ') as PrettyNames from people
            """,
            new CompilationOptions());

        AssertTypedAggregateContext(result.ExecutionPlanText);
        Assert.Contains("TypedAggregateSet [", result.ExecutionPlanText);
        AssertGeneratedCSharpContains(
            "AggregateValuesStringKernel.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "AggregateValuesStringDelimitedKernel.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "AggregateValuesStringKernel.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "AggregateValuesStringDelimitedKernel.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".GetValue<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForAggregateOnly_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select Count(1) as Count from #system.dual() d",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1L, table[0][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAggregateOnly_ShouldUseExecutionBackend()
    {
        var result = Inspect("select Count(1) as Count from #system.dual() d");

        AssertUsesExecutionBackend(result);
        AssertTypedAggregateContext(result.ExecutionPlanText);
        AssertGeneratedCSharpContains("group.__agg0.Count = checked(group.__agg0.Count + 1L)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__agg0.Count", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForSingleKeyAggregate_ShouldEmitExecutionAggregateCode()
    {
        var result = Inspect(
            "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy",
            new CompilationOptions());

        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        AssertExecutionPlanContains("GetOrAddSingleKeyAggregateGroup [group = groups[d.Dummy] by d.Dummy; typed:", result.ExecutionPlanText);
        AssertExecutionPlanContains("finalGroup.d.Dummy", result.ExecutionPlanText);
        Assert.Contains("new Dictionary<string, ResultAggregateGroup>()", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__key0", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForSingleKeyAggregate_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGroupedAggregate_ShouldUseExecutionBackend()
    {
        var result = Inspect("select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy");

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        AssertExecutionPlanContains("GetOrAddSingleKeyAggregateGroup [group = groups[d.Dummy] by d.Dummy; typed:", result.ExecutionPlanText);
        Assert.Contains("new Dictionary<string, ResultAggregateGroup>()", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__key0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenParallelizationModeIsFullForMergeableSingleKeyAggregate_ShouldEmitParallelAggregateLoop()
    {
        var result = Inspect(
            "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy",
            new CompilationOptions(parallelizationMode: ParallelizationMode.Full));

        AssertUsesExecutionBackend(result);
        Assert.Contains("ParallelSingleKeyAggregateLoop [d in dRows by d.Dummy; threshold 4096, sample 8192/6144", result.ExecutionPlanText);
        Assert.Contains("ParallelAccumulate", result.ExecutionPlanText);
        Assert.Contains("SerialFallback", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("EvaluationHelper.GetParallelAggregationRowsOrEmpty", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("EvaluationHelper.ShouldUseParallelSingleKeyAggregation", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("ParallelSingleKeyAggregate_0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("SerialSingleKeyAggregate_0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("Parallel.For(0, workerCount, options, worker.Run);", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("private static void ParallelSingleKeyAggregateShard_0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("private sealed class ParallelSingleKeyAggregateWorker_0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("mergedGroupRef.MergeFrom(sourceGroup)", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("shardIndex =>", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.AggregateSingleKeyParallel", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenParallelizationModeIsNoneForMergeableSingleKeyAggregate_ShouldKeepSerialAggregateLoop()
    {
        var result = Inspect(
            "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy",
            new CompilationOptions(parallelizationMode: ParallelizationMode.None));

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ParallelSingleKeyAggregateLoop", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.AggregateSingleKeyParallel", StringComparison.Ordinal));
        AssertExecutionPlanContains("GetOrAddSingleKeyAggregateGroup [group = groups[d.Dummy] by d.Dummy; typed:", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderAggregateOverHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateAggregateOverHashJoinQuery());

        Assert.Contains("PhysicalHashJoin [Inner] [build: e.Dummy] [probe: d.Dummy]", result.PhysicalPlanText);
        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("CreateHash [eHash: string -> DualEntity]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("ForEach [de in _tableResults[0].Rows]", StringComparison.Ordinal));
        Assert.Contains("HashProbe [eHash[d.Dummy] -> eHashMatches]", result.ExecutionPlanText);
        AssertTypedAggregateSet(result.ExecutionPlanText);
        AssertGeneratedCSharpContains("eHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("group.__agg0.Count = checked(group.__agg0.Count + 1L)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderAggregateOverHashJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateAggregateOverHashJoinQuery());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCteBackedAggregateOverHashJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateCteBackedAggregateOverHashJoinQuery());

        AssertUsesExecutionBackend(result);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("StoreTable [cte1 -> _cteRowResults.Slot1", result.ExecutionPlanText);
        AssertExecutionPlanContains("CreateHash [rHash: string -> Row; capacity: _cteRowResults.Slot1.Count]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[2]]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("ForEach [lr in _tableResults[2].Rows]", StringComparison.Ordinal));
        Assert.Contains("HashProbe [rHash[l.Dummy] -> rHashMatches]", result.ExecutionPlanText);
        AssertTypedAggregateSet(result.ExecutionPlanText);
        AssertGeneratedCSharpContains("rHash.TryGetValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("group.__agg0.Count = checked(group.__agg0.Count + 1L)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCteBackedAggregateOverHashJoin_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(CreateCteBackedAggregateOverHashJoinQuery());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForGroupedHaving_ShouldEmitExecutionAggregateGuard()
    {
        var result = Inspect(
            CreateGroupedHavingQuery(),
            new CompilationOptions());

        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        Assert.Contains("If [", result.ExecutionPlanText);
        Assert.Contains("Count(", result.ExecutionPlanText);
        AssertExecutionPlanContains("finalGroup.d.Dummy = 'single'", result.ExecutionPlanText);
        Assert.Contains("if (", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__key0", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForGroupedHaving_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            CreateGroupedHavingQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForComputedSingleKeyAggregateProjection_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy as Dummy, Count(1) + 1 as Count from #system.dual() d group by d.Dummy",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForGroupedAggregateOrderBy_ShouldEmitExecutionAggregateSort()
    {
        var result = Inspect(
            CreateGroupedAggregateOrderByQuery(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        Assert.Contains("SortShapeRows [result -> resultSorted by Count DESC]", result.ExecutionPlanText);
        Assert.Contains(
            "var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create",
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("private sealed class ResultRow0OrderBy_1DComparer : IComparer<ResultRow0>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForGroupedAggregateOrderBy_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            CreateGroupedAggregateOrderByQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForComputedGroupKey_ShouldEmitExecutionAggregateCode()
    {
        var result = Inspect(
            CreateComputedGroupKeyQuery(),
            new CompilationOptions());

        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        AssertExecutionPlanContains("GetOrAddSingleKeyAggregateGroup [group = groups[(d.Dummy || '!')] by d.Dummy + !; typed:", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("finalGroup.__key0", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForComputedGroupKey_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            CreateComputedGroupKeyQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single!", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForValueTupleAggregate_ShouldEmitExecutionAggregateCode()
    {
        var result = Inspect(
            "select d.Dummy as Dummy, 1 as One, Count(1) as Count from #system.dual() d group by d.Dummy, One",
            new CompilationOptions());

        AssertTypedValueTupleAggregateContext(result.ExecutionPlanText);
        AssertExecutionPlanContains("GetOrAddValueTupleAggregateGroup [group = groups[(d.Dummy, 1)] by d.Dummy, One; typed:", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("finalGroup.__key0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__key1", result.GeneratedCSharpCode);
        Assert.Contains("new Dictionary<(string, int), ResultAggregateGroup>()", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForValueTupleAggregate_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            "select d.Dummy as Dummy, 1 as One, Count(1) as Count from #system.dual() d group by d.Dummy, One",
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenExecutionIrRendererIsEnabledForWideValueTupleAggregate_ShouldEmitExecutionAggregateCode()
    {
        var result = Inspect(
            CreateWideValueTupleAggregateQuery(),
            new CompilationOptions());

        AssertExecutionPlanContains("AggregateGroup [ResultAggregateGroup; keys: 9; typed aggs: 1]", result.ExecutionPlanText);
        AssertExecutionPlanContains("CreateValueTupleAggregateContext [groups: (string, int, int, int, int, int, int, int, int) -> ResultAggregateGroup]", result.ExecutionPlanText);
        AssertExecutionPlanContains("GetOrAddValueTupleAggregateGroup [group = groups[(d.Dummy, 1, 2, 3, 4, 5, 6, 7, 8)] by d.Dummy, One, Two, Three, Four, Five, Six, Seven, Eight; typed: ResultAggregateGroup]", result.ExecutionPlanText);
        Assert.Contains("new Dictionary<(string, int, int, int, int, int, int, int, int), ResultAggregateGroup>()", result.GeneratedCSharpCode);
        Assert.DoesNotContain("GroupKey", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("new ResultAggregateGroup(groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7, groupKey8)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("group.__agg0.Count = checked(group.__agg0.Count + 1L)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__key0", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("finalGroup.__key8", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("finalGroup.GetValue<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForExecution_WhenExecutionIrRendererIsEnabledForWideValueTupleAggregate_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            CreateWideValueTupleAggregateQuery(),
            new CompilationOptions());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(8, table[0][8]);
        Assert.AreEqual(1L, table[0][9]);
    }

}
