using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void OrderByGeneratedSamples_WhenCheckedIn_ShouldUseTypedRecordPipeline()
    {
        var samples = ReadNamedSamples(
            OrderBySimpleSampleFileName,
            OrderByMultipleKeysSampleFileName,
            OrderByAliasSampleFileName,
            OrderByHiddenComputedKeySampleFileName);

        foreach (var sample in samples)
        {
            Assert.Contains("GeneratedRecord [ResultRow0WithSortKeys]", sample.Content);
            Assert.Contains("CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]", sample.Content);
            Assert.Contains("OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys", sample.Content);
            Assert.Contains("MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0", sample.Content);
            Assert.Contains("private sealed class ResultRow0WithSortKeys", sample.Content);
            Assert.Contains("private sealed class ResultRow0 : Row", sample.Content);
            Assert.IsFalse(sample.Content.Contains("ResultRow0WithSortKeys : Row", StringComparison.Ordinal));
            Assert.IsFalse(sample.Content.Contains("result.Rows.OrderBy", StringComparison.Ordinal));
            Assert.IsFalse(sample.Content.Contains("RowOrderKey", StringComparison.Ordinal));
            Assert.IsFalse(sample.Content.Contains("ProjectTable [", StringComparison.Ordinal));
            Assert.IsFalse(sample.Content.Contains("resultSourceRow[", StringComparison.Ordinal));
            Assert.IsFalse(sample.Content.Contains("resultSourceRow.Contexts", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void FramedPluginWindowSample_WhenCheckedIn_ShouldStreamWithoutValueBuffer()
    {
        var samples = ReadNamedSamples(WindowRunningProductFramedPluginSampleFileName)
            .ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var sample = samples[WindowRunningProductFramedPluginSampleFileName];

        Assert.Contains(
            "new Musoq.Evaluator.Tests.Schema.Basic.Library().WindowRunningProduct()",
            sample);
        Assert.Contains(
            "resultRunningProductsFunction.Accumulate((decimal?)ko3iko.Population);",
            sample);
        Assert.Contains("resultRunningProductsFunction.GetValue();", sample);
        Assert.Contains("var resultRunningProducts = new decimal[resultWindowRows.Count];", sample);
        Assert.IsFalse(sample.Contains("resultRunningProductsFunction.SetArguments(Array.Empty<object?>());", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultRunningProductsFunction.AccumulateValue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultRunningProductsFunction.GetCurrentValue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("var resultRunningProductsLibraryBase0 = new Musoq.Plugins.LibraryBase();", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase()", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase().ToDecimal", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeFramedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultRunningProductsValues", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NonDeterministicPluginWindowValue_WhenCompiledForInspection_ShouldNotReuseLibraryTarget()
    {
        var result = CompileBasicQueryForInspection(
            "select Sum(Rand()) over () as total from #A.entities()");

        Assert.Contains(
            "new Musoq.Plugins.LibraryBase().Rand()",
            result.GeneratedCSharpCode);
        Assert.IsFalse(
            result.GeneratedCSharpCode.Contains(
                "var resultSumsLibraryBase0 = new Musoq.Plugins.LibraryBase();",
                StringComparison.Ordinal),
            "Non-deterministic LibraryBase methods should keep their existing per-invocation target shape.");
    }

    [TestMethod]
    public void TypedAggregateInputMethod_WhenCompiledForInspection_ShouldReuseLibraryTarget()
    {
        var result = CompileBasicQueryForInspection(
            "select City, Sum(Length(Name)) from #A.entities() group by City");

        Assert.Contains(
            "var libraryBase0 = new Musoq.Plugins.LibraryBase();",
            result.GeneratedCSharpCode);
        Assert.Contains(
            "libraryBase0.Length<char>",
            result.GeneratedCSharpCode);
        Assert.IsFalse(
            result.GeneratedCSharpCode.Contains(
                "new Musoq.Plugins.LibraryBase().Length",
                StringComparison.Ordinal),
            "Typed aggregate inputs should not allocate LibraryBase inside the row loop.");
    }

    [TestMethod]
    public void ParallelSingleKeyAggregateHelper_WhenAggregateInputUsesLibraryTarget_ShouldReceiveTargetParameter()
    {
        var result = CompileBasicQueryForInspection(
            "select a.Country, AggregateValues(GetElementAt(a.Country, 0)) from #A.entities() a group by a.Country");

        Assert.Contains(
            "var libraryBase0 = new Musoq.Plugins.LibraryBase();",
            result.GeneratedCSharpCode);

        const string parallelAggregateCall = "ParallelSingleKeyAggregate_0(";
        if (result.GeneratedCSharpCode.Contains(parallelAggregateCall, StringComparison.Ordinal))
        {
            var callLine = GetLine(
                result.GeneratedCSharpCode,
                result.GeneratedCSharpCode.IndexOf(parallelAggregateCall, StringComparison.Ordinal));

            Assert.Contains(", token, libraryBase0)", callLine);
            Assert.Contains("Musoq.Plugins.LibraryBase libraryBase0", result.GeneratedCSharpCode);
        }
        else
        {
            Assert.Contains(
                "UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, libraryBase0,",
                result.GeneratedCSharpCode);
            Assert.Contains(
                "Musoq.Plugins.LibraryBase libraryBase0",
                result.GeneratedCSharpCode);
        }
    }

    [TestMethod]
    public void GroupBySingleSample_WhenCheckedIn_ShouldReuseGroupKeyFieldReadForAggregateInput()
    {
        var sample = ReadSample(GroupBySingleSampleFileName).Content;

        Assert.Contains("string city = ko3iko.City;", sample);
        Assert.Contains("string groupKey = city;", sample);
        Assert.Contains("private sealed class ResultAggregateGroup", sample);
        Assert.IsFalse(sample.Contains("private sealed class ResultAggregateGroup : Group", StringComparison.Ordinal));
        Assert.Contains("public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg0", sample);
        Assert.Contains("ParallelSingleKeyAggregateLoop [ko3iko in ko3ikoRows by ko3iko.City; threshold 4096, sample 8192/6144", sample);
        Assert.DoesNotContain("EvaluationHelper.GetParallelAggregationRowsOrEmpty<", sample);
        Assert.Contains("ParallelSingleKeyAggregate_0", sample);
        Assert.DoesNotContain("EvaluationHelper.ShouldUseParallelSingleKeyAggregation<", sample);
        Assert.DoesNotContain("SerialSingleKeyAggregate", sample);
        Assert.Contains(
            "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]",
            sample);
        Assert.Contains(
            "private static List<ResultAggregateGroup> ParallelSingleKeyAggregate_0(",
            sample);
        Assert.Contains("Parallel.ForEach<IReadOnlyList<", sample);
        Assert.Contains("private static void ParallelSingleKeyAggregateChunk_0(", sample);
        Assert.Contains("private sealed class ParallelSingleKeyAggregateChunkWorker_0", sample);
        Assert.DoesNotContain("var worker = new ParallelSingleKeyAggregateWorker_0(", sample);
        Assert.DoesNotContain("Parallel.For(0, workerCount, options, worker.Run);", sample);
        Assert.DoesNotContain("private static void ParallelSingleKeyAggregateShard_0(", sample);
        Assert.DoesNotContain("private sealed class ParallelSingleKeyAggregateWorker_0", sample);
        Assert.IsFalse(sample.Contains("shardIndex =>", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.AggregateSingleKeyParallel<", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("rootGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("CreateAggregateLibrary [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("var libraryBase0 = new Musoq.Plugins.LibraryBase();", StringComparison.Ordinal));
        Assert.Contains("group.__agg0.Count = checked(group.__agg0.Count + 1L);", sample);
        Assert.Contains("mergedGroupRef.MergeFrom(sourceGroup)", sample);
        Assert.Contains("public void MergeFrom(ResultAggregateGroup source)", sample);
        Assert.Contains("finalGroup.__agg0.Count", sample);
        Assert.IsFalse(sample.Contains(
            "libraryBase0.SetCount(group, \"ko3iko.Count(ko3iko.City)\", city, 0);",
            StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("_tableResults[0].Rows", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TypedValueTupleAggregateSamples_WhenCheckedIn_ShouldNotBuildLegacyParentGroupChains()
    {
        var failures = ReadAllSamples()
            .Where(static sample =>
                sample.Content.Contains("CreateValueTupleAggregateContext [groups:", StringComparison.Ordinal) &&
                sample.Content.Contains("-> ResultAggregateGroup]", StringComparison.Ordinal))
            .SelectMany(static sample =>
            {
                var failures = new List<string>();
                if (sample.Content.Contains("var rootGroup = new Group(", StringComparison.Ordinal))
                    failures.Add($"{sample.FileName}: still creates legacy aggregate root group");
                if (sample.Content.Contains("new Group(parent", StringComparison.Ordinal))
                    failures.Add($"{sample.FileName}: still creates legacy parent group chain");

                return failures;
            })
            .ToArray();

        Assert.IsEmpty(failures, string.Join(", ", failures));
    }

    [TestMethod]
    public void GroupByHavingOrderBySample_WhenCheckedIn_ShouldYieldFinalShapeRows()
    {
        var sample = ReadSample(GroupByHavingOrderBySampleFileName).Content;

        Assert.Contains(StaticColumnMetadataPattern, sample);
        Assert.IsFalse(sample.Contains("var resultSorted = new Table(\"resultSorted\", __columns_", StringComparison.Ordinal));
        Assert.Contains("var __musoqFinalShapeRows = new List<ResultShape0>();", sample);
        Assert.Contains(
            "var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create",
            sample);
        Assert.Contains("__musoqFinalShapeRows.Add(resultSortedRowsRow);", sample);
        Assert.Contains("return __musoqFinalShapeRows;", sample);
        Assert.IsFalse(sample.Contains("private sealed class ResultRow0OrderBy_2DComparer : IComparer<ResultRow0>", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.Columns.ToArray()", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.Rows.OrderByDescending((row) => (decimal?)row[2])", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("((ResultRow0)row).Sum_Population_", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OrderBySkipTakeSample_WhenCheckedIn_ShouldUseTopOffsetTable()
    {
        var sample = ReadSample(OrderBySkipTakeSampleFileName).Content;

        Assert.Contains("CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by Population DESC, skip 2, take 5]", sample);
        Assert.Contains(
            "var resultOrderRecords = new EvaluationHelper.BoundedTopRecordList<ResultRow0WithSortKeys>(2, 5, ResultRow0WithSortKeysComparer.Instance);",
            sample);
        Assert.Contains(
            "resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Name, ko3iko.Population, resultOrderRecords.Count));",
            sample);
        Assert.Contains("private readonly struct ResultRow0WithSortKeys", sample);
        Assert.IsFalse(sample.Contains("OrderRecordList [resultOrderRecords", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.SelectTopOffsetRecords(resultOrderRecords", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TopOffsetTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("RowOrderKey", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("AppendTopOffsetRowsDirect", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.Rows.OrderBy", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultTopOffsetRows", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("var resultSorted =", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultSortedSliced", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultSortedSkipped", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultSortedSkippedTaken", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OrderByTopOffsetHiddenKeySample_WhenCheckedIn_ShouldPruneHiddenOrderKeyFromFinalRows()
    {
        var sample = ReadSample(OrderByTopOffsetHiddenKeySampleFileName).Content;
        var normalizedSample = sample.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("GeneratedRecord [ResultRow0WithSortKeys]", sample);
        Assert.Contains("__sortKey0: decimal <- field __sortKey0", sample);
        Assert.Contains("Generated [ResultRow0]\n      Name: string <- field Name", normalizedSample);
        Assert.Contains("MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0]", sample);
        Assert.Contains("CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by __sortKey0 DESC, skip 1, take 3]", sample);
        Assert.Contains("resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Name, (ko3iko.Population + ko3iko.Money), resultOrderRecords.Count));", sample);
        Assert.Contains("private readonly struct ResultRow0WithSortKeys", sample);
        Assert.Contains("private sealed class ResultRow0 : Row", sample);
        Assert.IsFalse(normalizedSample.Contains("Generated [ResultRow0]\n      __sortKey0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("private sealed class ResultRow0WithSortKeys : Row", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TopOffsetTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.Rows.OrderBy", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompilationPipelineSamples_WhenCheckedIn_ShouldExposeSimpleAndComplexCompileShapes()
    {
        var samples = ReadNamedSamples(CompilationSimpleSelectSampleFileName, CompilationComplexGroupedSortSampleFileName)
            .ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var simple = samples[CompilationSimpleSelectSampleFileName];
        var complex = samples[CompilationComplexGroupedSortSampleFileName];

        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", simple);
        Assert.Contains("Let [population: decimal = ko3iko.Population]", simple);
        Assert.Contains("If [(population > 500000)]", simple);
        Assert.Contains("yield return new ResultShape0(ko3iko.City, ko3iko.Country, population);", simple);
        Assert.Contains("yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.Country, __musoqShapeRow.Population);", simple);
        Assert.DoesNotContain("private Table ComputeTable_compiled_0(", simple);
        Assert.IsFalse(simple.Contains(ParallelFilterProjectLoopPattern, StringComparison.Ordinal));
        Assert.IsFalse(simple.Contains(ParallelProjectionRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(simple.Contains(ParallelProjectRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(simple.Contains("Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(simple.Contains("_tableResults[0]", StringComparison.Ordinal));

        Assert.Contains("AggregateGroup [ResultAggregateGroup; keys: 3; typed aggs: 1]", complex);
        Assert.Contains("CreateValueTupleAggregateContext [groups: (string, string, decimal) -> ResultAggregateGroup]", complex);
        Assert.Contains("private sealed class ResultAggregateGroup", complex);
        Assert.Contains("var result = new List<ResultShape0>();", complex);
        Assert.Contains("result.Add(new ResultShape0(", complex);
        Assert.IsFalse(complex.Contains("private sealed class ResultAggregateGroup : Group", StringComparison.Ordinal));
        Assert.IsFalse(complex.Contains("void PopulateResultGroups()", StringComparison.Ordinal));
        Assert.IsFalse(complex.Contains("void FinalizeResultGroups()", StringComparison.Ordinal));
        Assert.Contains("public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg0", complex);
        Assert.Contains("SortShapeRows [result -> resultSorted by Population DESC]", complex);
        Assert.Contains(
            "var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create",
            complex);
        Assert.IsFalse(complex.Contains("private sealed class ResultRow0OrderBy_2DComparer : IComparer<ResultRow0>", StringComparison.Ordinal));
        Assert.IsFalse(complex.Contains("Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(complex.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.IsFalse(complex.Contains("result.Rows.OrderByDescending((row) => (decimal)row[2])", StringComparison.Ordinal));
        Assert.IsFalse(complex.Contains("((ResultRow0)row).Population", StringComparison.Ordinal));
        Assert.IsFalse(complex.Contains(".SetCount(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GroupBySkipTakeSample_WhenCheckedIn_ShouldFinalizeDirectlyIntoSlicedResult()
    {
        var sample = ReadSample(GroupBySkipTakeSampleFileName).Content;

        Assert.Contains("SliceShapeRows [result -> resultSliced, skip 1, take 3]", sample);
        Assert.Contains("var resultSlicedRows = result.Skip(1).Take(3);", sample);
        Assert.IsFalse(sample.Contains("Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("_tableResults[0].Rows", StringComparison.Ordinal));
    }
}
