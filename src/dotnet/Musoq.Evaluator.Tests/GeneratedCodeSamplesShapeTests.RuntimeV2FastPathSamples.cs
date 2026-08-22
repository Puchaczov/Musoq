using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CompilationCseDisabledSample_WhenCheckedIn_ShouldKeepRepeatedFieldReadsInline()
    {
        var sample = ReadSample(CompilationCseDisabledSampleFileName).Content;

        Assert.Contains("If [(ko3iko.Population = ko3iko.Population)]", sample);
        Assert.Contains("(ko3iko) => (ko3iko.Population == ko3iko.Population)", sample);
        Assert.Contains("TableProjectionRows.ProjectRowsSerial<", sample);
        Assert.Contains("new ResultRow0(ko3iko.Population, ko3iko.Population)", sample);
        Assert.IsFalse(sample.Contains("Let [population:", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("decimal population = ko3iko.Population;", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompilationCseEnabledSample_WhenCheckedIn_ShouldHoistRepeatedFieldReads()
    {
        var sample = ReadSample(CompilationCseEnabledSampleFileName).Content;

        Assert.Contains("Let [population: decimal = ko3iko.Population]", sample);
        Assert.Contains("If [(population = population)]", sample);
        Assert.Contains("decimal population = ko3iko.Population;", sample);
        Assert.Contains("if ((population == population))", sample);
        Assert.Contains("yield return new ResultShape0(population, population);", sample);
        Assert.Contains("yield return new ResultRow0(__musoqShapeRow.LeftPopulation, __musoqShapeRow.RightPopulation);", sample);
    }

    [TestMethod]
    public void RuntimeV2CseNoDuplicateRegressionSample_WhenCheckedIn_ShouldExposeDirectLoopProjectionPath()
    {
        var sample = ReadSample(RuntimeV2CseNoDuplicateRegressionSampleFileName)
            .Content;

        Assert.Contains("SELECT Value * 2, Name", sample);
        Assert.Contains("SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows", sample);
        Assert.Contains("ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (ExpensiveMethod(ko3iko.Value) > 100)", sample);
        Assert.Contains("Let [value: int = ko3iko.Value]", sample);
        Assert.Contains("If [(ExpensiveMethod(value) > 100)]", sample);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>", sample);
        Assert.Contains("QueryRows.FromRowShards(", sample);
        Assert.Contains("__resultRuntimeV2RegressionLibrary0.ExpensiveMethod(ko3iko.Value)", sample);
        Assert.Contains("new ResultRow0((ko3iko.Value * 2), ko3iko.Name)", sample);
        Assert.IsFalse(sample.Contains("GetOrAddCachedMethod", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Execution IR does not support this query shape", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TableRowSource", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2WeatherSingleAggregateSample_WhenCheckedIn_ShouldStreamCastAndFuseInlineAggregates()
    {
        var sample = ReadSample(RuntimeV2WeatherSingleAggregateSampleFileName)
            .Content;

        Assert.Contains("ParallelSingleKeyAggregate_0(ko3ikoRows, 24, token);", sample);
        Assert.Contains("Parallel.ForEach<IReadOnlyList<", sample);
        Assert.DoesNotContain("EvaluationHelper.GetParallelAggregationRowsOrEmpty", sample);
        Assert.Contains("float? temperatureSingle = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToSingle(temperature);", sample);
        Assert.Contains("var __agg0Input = (float?)temperatureSingle;", sample);
        Assert.DoesNotContain("var __agg1Input", sample);
        Assert.DoesNotContain("var __agg2Input", sample);
        Assert.AreEqual(1, CountOccurrences(sample, "GetValueOrDefault();"));
        Assert.Contains("group.__agg0.Sum = group.__agg0.HasValue ? checked(group.__agg0.Sum + __agg0Current) : __agg0Current;", sample);
        Assert.Contains("if (!group.__agg1.HasValue || __agg0Current > group.__agg1.Value)", sample);
        Assert.Contains("if (!group.__agg2.HasValue || __agg0Current < group.__agg2.Value)", sample);
    }

    [TestMethod]
    public void RuntimeV2WindowRunningSumSample_WhenCheckedIn_ShouldUseRenderedAggregateKernel()
    {
        var sample = ReadSample(RuntimeV2WindowRunningSumSampleFileName)
            .Content;

        Assert.Contains("Sum(ToDecimal(Salary)) over (partition by Department order by Salary)", sample);
        Assert.Contains("ComputeSumWindowKernel[BoundedRows]", sample);
        Assert.Contains("var resultSumsPartitionBuilder = new Musoq.Evaluator.Helpers.WindowPartitionBuilder<string>(resultWindowRows.Count);", sample);
        Assert.Contains("var resultSumsOrderKeys = new WindowResultSumsOrderKeysKey[resultWindowRows.Count];", sample);
        Assert.Contains("WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumsPartitions, resultSumsOrderKeys, false);", sample);
        Assert.Contains("WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSumsOrderKeys", sample);
        Assert.Contains("var resultSumsPrefixSum = System.Buffers.ArrayPool<decimal>.Shared.Rent", sample);
        Assert.Contains("var resultSums = new decimal[resultWindowRows.Count];", sample);
        Assert.Contains("resultSums[resultSumsCurrentIndex] = resultSumsPrefixSum[resultSumsFramePrefixEnd] - resultSumsPrefixSum[resultSumsFramePrefixStart];", sample);
        Assert.IsFalse(sample.Contains("resultSumsIntOrderBuilder", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("var resultSumsPartitionKeys = new string[resultWindowRows.Count];", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Execution IR does not support this query shape", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputePluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("0.0 us", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2WindowQualifyRankSample_WhenCheckedIn_ShouldRenderRankingAndFilter()
    {
        var sample = ReadSample(RuntimeV2WindowQualifyRankSampleFileName)
            .Content;

        Assert.Contains("QUALIFY Rank() over (partition by Department order by Salary desc) <= 3", sample);
        Assert.Contains("ComputeRankWindow [resultRanks", sample);
        Assert.Contains("qualify <= 3", sample);
        Assert.Contains("var resultRanksIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder<string>(resultWindowRows.Count);", sample);
        Assert.Contains("resultRanksIntOrderBuilder.Add((string)ko3iko.Department, (int)ko3iko.Salary, windowIndex);", sample);
        Assert.Contains("var resultRanksPartitions = resultRanksIntOrderBuilder.ToSortedPartitionSet(true);", sample);
        Assert.Contains("var resultRanks = resultRanksIntOrderBuilder.ComputeRankTopN(resultRanksPartitions, 3L);", sample);
        Assert.Contains("if ((((long)resultRanks", sample);
        Assert.Contains("> 0L", sample);
        Assert.Contains("<= 3))", sample);
        Assert.IsFalse(sample.Contains("resultRanksPartitionKeys", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultRanksOrderKeys", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowResultRanksOrderKeysKey", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeRank<int>", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeRank", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Execution IR does not support this query shape", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputePluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("0.0 us", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2BenchmarkParitySamples_WhenCheckedIn_ShouldCoverSlowerGeneratedShapes()
    {
        var samples = ReadNamedSamples(
                RuntimeV2WindowBenchmarkRowNumberNoPartitionSampleFileName,
                RuntimeV2WindowBenchmarkRowNumberPartitionedSampleFileName,
                RuntimeV2WindowBenchmarkRankPartitionedSampleFileName,
                RuntimeV2WindowBenchmarkDenseRankPartitionedSampleFileName,
                RuntimeV2WindowBenchmarkCountWholePartitionSampleFileName,
                RuntimeV2ParallelTableAddBenchmarkSampleFileName)
            .ToDictionary(static item => item.FileName, static item => item.Content);
        var rowNumberNoPartition = samples[RuntimeV2WindowBenchmarkRowNumberNoPartitionSampleFileName];
        var rowNumberPartitioned = samples[RuntimeV2WindowBenchmarkRowNumberPartitionedSampleFileName];
        var rankPartitioned = samples[RuntimeV2WindowBenchmarkRankPartitionedSampleFileName];
        var denseRankPartitioned = samples[RuntimeV2WindowBenchmarkDenseRankPartitionedSampleFileName];
        var countWholePartition = samples[RuntimeV2WindowBenchmarkCountWholePartitionSampleFileName];
        var parallelTableAdd = samples[RuntimeV2ParallelTableAddBenchmarkSampleFileName];

        Assert.Contains("RowNumber() over (order by Salary desc)", rowNumberNoPartition);
        Assert.Contains("ComputeRowNumberWindow [", rowNumberNoPartition);
        Assert.Contains("var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];", rowNumberNoPartition);
        Assert.Contains("resultRowNumbers[resultRowNumbersCurrentIndex] = resultRowNumbersPartitionIndex + 1L;", rowNumberNoPartition);
        Assert.IsFalse(rowNumberNoPartition.Contains("resultRowNumbersIntOrderBuilder", StringComparison.Ordinal));

        Assert.Contains("RowNumber() over (partition by Department order by Salary desc)", rowNumberPartitioned);
        Assert.Contains("ComputeRowNumberWindow [", rowNumberPartitioned);
        Assert.Contains("var resultRowNumbersPartitionKeys = new string[resultWindowRows.Count];", rowNumberPartitioned);
        Assert.Contains("var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];", rowNumberPartitioned);
        Assert.Contains("resultRowNumbers[resultRowNumbersCurrentIndex] = resultRowNumbersPartitionIndex + 1L;", rowNumberPartitioned);
        Assert.IsFalse(rowNumberPartitioned.Contains("resultRowNumbersIntOrderBuilder", StringComparison.Ordinal));

        Assert.Contains("Rank() over (partition by Department order by Salary desc)", rankPartitioned);
        Assert.Contains("ComputeRankWindow [", rankPartitioned);
        Assert.Contains("var resultRanksIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder<string>(resultWindowRows.Count);", rankPartitioned);
        Assert.Contains("resultRanksIntOrderBuilder.Add((string)ko3iko.Department, (int)ko3iko.Salary, windowIndex);", rankPartitioned);
        Assert.Contains("var resultRanksPartitions = resultRanksIntOrderBuilder.ToSortedPartitionSet(true);", rankPartitioned);
        Assert.Contains("var resultRanks = resultRanksIntOrderBuilder.ComputeRank(resultRanksPartitions);", rankPartitioned);
        Assert.IsFalse(rankPartitioned.Contains("resultRanksPartitionKeys", StringComparison.Ordinal));
        Assert.IsFalse(rankPartitioned.Contains("resultRanksOrderKeys", StringComparison.Ordinal));
        Assert.IsFalse(rankPartitioned.Contains("WindowResultRanksOrderKeysKey", StringComparison.Ordinal));
        Assert.IsFalse(rankPartitioned.Contains("WindowFunctionHelpers.ComputeRank", StringComparison.Ordinal));

        Assert.Contains("DenseRank() over (partition by Department order by Salary desc)", denseRankPartitioned);
        Assert.Contains("ComputeDenseRankWindow [", denseRankPartitioned);
        Assert.Contains("var resultDenseRanksIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder<string>(resultWindowRows.Count);", denseRankPartitioned);
        Assert.Contains("resultDenseRanksIntOrderBuilder.Add((string)ko3iko.Department, (int)ko3iko.Salary, windowIndex);", denseRankPartitioned);
        Assert.Contains("var resultDenseRanksPartitions = resultDenseRanksIntOrderBuilder.ToSortedPartitionSet(true);", denseRankPartitioned);
        Assert.Contains("var resultDenseRanks = resultDenseRanksIntOrderBuilder.ComputeDenseRank(resultDenseRanksPartitions);", denseRankPartitioned);
        Assert.IsFalse(denseRankPartitioned.Contains("resultDenseRanksPartitionKeys", StringComparison.Ordinal));
        Assert.IsFalse(denseRankPartitioned.Contains("resultDenseRanksOrderKeys", StringComparison.Ordinal));
        Assert.IsFalse(denseRankPartitioned.Contains("WindowResultDenseRanksOrderKeysKey", StringComparison.Ordinal));
        Assert.IsFalse(denseRankPartitioned.Contains("WindowFunctionHelpers.ComputeDenseRank", StringComparison.Ordinal));

        Assert.Contains("Count(Name) over (partition by Department)", countWholePartition);
        Assert.Contains("ComputeCountWindowKernel[WholePartition]", countWholePartition);
        Assert.Contains("WindowPartitionCountBuilder<string>", countWholePartition);
        Assert.Contains("resultCountsPartitionCountBuilder.AddReferenceUnchecked(ko3iko.Department, ko3iko.Name != null, windowIndex)", countWholePartition);
        Assert.Contains("var resultCounts = resultCountsPartitionCountBuilder.ToResultInPlaceUnchecked()", countWholePartition);
        Assert.IsFalse(countWholePartition.Contains("WindowPartitionBuilder<string>", StringComparison.Ordinal));
        Assert.IsFalse(countWholePartition.Contains("resultCountsValue", StringComparison.Ordinal));
        Assert.IsFalse(countWholePartition.Contains("WindowFunctionHelpers.ComputePluginWindowFunction", StringComparison.Ordinal));

        Assert.Contains("SELECT Id, Name, Value, Category, HeavyComputation(Value) as Heavy", parallelTableAdd);
        Assert.Contains(ParallelFilterProjectLoopPattern, parallelTableAdd);
        Assert.Contains(ParallelProjectionRowsPattern, parallelTableAdd);
        Assert.Contains(TableParallelProjectRowsPattern, parallelTableAdd);
        Assert.Contains(AddRowsDirectPattern, parallelTableAdd);
    }

    [TestMethod]
    public void ValuesSamples_WhenCheckedIn_ShouldUseTypedRowsAndReuseCteMaterialization()
    {
        var samples = ReadNamedSamples(ValuesRowLiteralsSampleFileName, ValuesCteReuseSampleFileName, ValuesNumericLiteralsSampleFileName)
            .ToDictionary(static item => item.FileName, static item => item.Content);
        var rowLiteralSample = samples[ValuesRowLiteralsSampleFileName];
        var cteReuseSample = samples[ValuesCteReuseSampleFileName];
        var numericLiteralSample = samples[ValuesNumericLiteralsSampleFileName];

        Assert.Contains("ValuesScan [2 rows as packages]", rowLiteralSample);
        Assert.Contains("PhysicalValuesScan [2 rows as packages]", rowLiteralSample);
        Assert.Contains("UnknownShape [ValuesRowShape]", rowLiteralSample);
        Assert.Contains("CreateValuesRows [packagesRows:", rowLiteralSample);
        Assert.Contains("packagesRows = new packagesValues", rowLiteralSample);
        Assert.Contains("private sealed class packagesValues", rowLiteralSample);
        Assert.Contains("public string Name { get; private set; }", rowLiteralSample);
        Assert.Contains("public bool Approved { get; private set; }", rowLiteralSample);
        Assert.Contains("public uint Score { get; private set; }", rowLiteralSample);
        Assert.IsFalse(rowLiteralSample.Contains("TableRowSource", StringComparison.Ordinal));
        Assert.IsFalse(rowLiteralSample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));

        Assert.Contains("ValuesScan [2 rows as p]", cteReuseSample);
        Assert.Contains("PhysicalValuesScan [2 rows as p]", cteReuseSample);
        Assert.Contains("CreateValuesRows [cte0_pRows:", cteReuseSample);
        Assert.Contains("private sealed class pValues", cteReuseSample);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]", cteReuseSample);
        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", cteReuseSample);
        Assert.Contains("foreach (var rightPolicy in __storedTable0Rows)", cteReuseSample);
        Assert.Contains("foreach (var leftPolicy in __storedTable0Rows)", cteReuseSample);
        Assert.AreEqual(1, CountOccurrences(cteReuseSample, "CreateValuesRows ["));
        Assert.IsFalse(cteReuseSample.Contains("TableRowSource", StringComparison.Ordinal));
        Assert.IsFalse(cteReuseSample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));

        Assert.Contains("ValuesScan [1 rows as literals]", numericLiteralSample);
        Assert.Contains("private sealed class literalsValues", numericLiteralSample);
        Assert.Contains("public int PlainInt { get; private set; }", numericLiteralSample);
        Assert.Contains("public uint UIntValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public long LongValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public ulong ULongValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public short ShortValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public ushort UShortValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public sbyte SByteValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public byte ByteValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public decimal DecimalValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public long HexValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public long BinaryValue { get; private set; }", numericLiteralSample);
        Assert.Contains("public long OctalValue { get; private set; }", numericLiteralSample);
    }

    [TestMethod]
    public void RuntimeV2SkipTakeNoOrderSample_WhenCheckedIn_ShouldStreamPaginationBeforeRowCreation()
    {
        var sample = ReadSample(RuntimeV2SkipTakeNoOrderSampleFileName)
            .Content;

        Assert.Contains("SELECT FirstName, LastName, Email", sample);
        Assert.Contains("SKIP 100 TAKE 100", sample);
        Assert.Contains("SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows", sample);
        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", sample);
        Assert.Contains("Let [__resultSkipRemaining: int = 100]", sample);
        Assert.Contains("Let [__resultTakeRemaining: int = 100]", sample);
        Assert.Contains("Continue", sample);
        Assert.Contains("Break", sample);
        Assert.DoesNotContain("result.EnsureCapacity(100);", sample);
        Assert.Contains("yield return new ResultShape0(ko3iko.FirstName, ko3iko.LastName, ko3iko.Email);", sample);
        Assert.Contains("yield return new ResultRow0(__musoqShapeRow.FirstName, __musoqShapeRow.LastName, __musoqShapeRow.Email);", sample);
        Assert.Contains("__resultTakeRemaining = (__resultTakeRemaining - 1);", sample);
        var shapeYield = sample.LastIndexOf("yield return new ResultShape0", StringComparison.Ordinal);
        var preAppendStop = sample.IndexOf("if ((__resultTakeRemaining <= 0))", StringComparison.Ordinal);
        var postAppendStop = sample.LastIndexOf("if ((__resultTakeRemaining <= 0))", StringComparison.Ordinal);
        Assert.IsTrue(preAppendStop < shapeYield, "The zero-take guard must run before row creation.");
        Assert.IsTrue(shapeYield < postAppendStop, "The accepted-row limit must stop before another chunk is requested.");
        Assert.IsFalse(sample.Contains("SkipTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TakeTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("SliceTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains(".Rows.Skip(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains(".Rows.Take(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2StringFilterSample_WhenCheckedIn_ShouldUseDirectStreamingPredicate()
    {
        var sample = ReadSample(RuntimeV2StringFilterSampleFileName)
            .Content;

        Assert.Contains("WHERE Contains(Email, 'gmail') AND StartsWith(FirstName, 'A')", sample);
        Assert.Contains("PhysicalSchemaScan [#test.entities() as ko3iko] [pushdown: Contains(ko3iko.Email, 'gmail'), StartsWith(ko3iko.FirstName, 'A')]", sample);
        Assert.Contains("Contains(\"gmail\", StringComparison.OrdinalIgnoreCase)", sample);
        Assert.Contains("StartsWith(\"A\", StringComparison.OrdinalIgnoreCase)", sample);
        Assert.Contains("new ResultRow0(ko3iko.FirstName, ko3iko.LastName, ko3iko.Email)", sample);
        Assert.Contains("QueryRows.FromRowShards(", sample);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<", sample);
        Assert.DoesNotContain("TableProjectionRows.ProjectOptionalRowsSerial<", sample);
        Assert.IsFalse(sample.Contains("CreateObject [__resultLibraryBase", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase()", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase().Contains", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase().StartsWith", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TableRowSource", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("SkipTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TakeTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("SliceTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2DeterministicMethodCseSample_WhenCheckedIn_ShouldUseRowLocalHoistedMethod()
    {
        var sample = ReadSample(RuntimeV2DeterministicMethodCseSampleFileName)
            .Content;

        Assert.Contains("SELECT ExpensiveCompute(Value) as Computed", sample);
        Assert.Contains("CASE WHEN ExpensiveCompute(Value) > 300 THEN 'High' ELSE 'Low' END as Bucket", sample);
        Assert.Contains("WHERE ExpensiveCompute(Value) > 50", sample);
        Assert.Contains("CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]", sample);
        Assert.Contains("Let [expensiveCompute: int = ExpensiveCompute(value)]", sample);
        Assert.Contains("If [(expensiveCompute > 50)]", sample);
        Assert.IsFalse(sample.Contains("System.Collections.Concurrent.ConcurrentDictionary<int, int>", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetOrAddCachedMethod<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionLibrary, int, int>", StringComparison.Ordinal));
        Assert.Contains("int expensiveCompute = (int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(value);", sample);
        Assert.Contains("if ((expensiveCompute > 50))", sample);
        Assert.Contains("new ResultRow0(expensiveCompute, (expensiveCompute + 10), (expensiveCompute > 300) ? (string)\"High\" : (string)\"Low\")", sample);
        Assert.Contains("QueryRows.FromRowShards(", sample);
        Assert.Contains("EvaluationHelper.ProjectChunkedRowsParallel<", sample);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<", sample);
        Assert.DoesNotContain("TableProjectionRows.ProjectOptionalRowsSerial<", sample);
        Assert.AreEqual(2, CountOccurrences(sample, "int expensiveCompute = (int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(value);"));
        Assert.IsFalse(sample.Contains("new Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionLibrary().ExpensiveCompute", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2DeterministicMethodCseDisabledSample_WhenCheckedIn_ShouldKeepRepeatedMethodCallsInline()
    {
        var sample = ReadSample(RuntimeV2DeterministicMethodCseDisabledSampleFileName)
            .Content;

        Assert.Contains("SELECT ExpensiveCompute(Value) as Computed", sample);
        Assert.Contains("CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]", sample);
        Assert.IsFalse(sample.Contains("Let [expensiveCompute:", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("int expensiveCompute =", StringComparison.Ordinal));
        Assert.IsGreaterThan(
1, CountOccurrences(sample, "__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value)"));
        Assert.IsFalse(sample.Contains("GetOrAddCachedMethod", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentDictionary", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
    }

}
