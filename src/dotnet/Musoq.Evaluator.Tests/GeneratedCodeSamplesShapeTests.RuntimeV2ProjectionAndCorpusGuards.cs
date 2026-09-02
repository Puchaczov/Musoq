using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RuntimeV2ParallelFilterProjectSample_WhenCheckedIn_ShouldUsePartitionLocalParallelBuffers()
    {
        var sample = ReadSample(RuntimeV2ParallelFilterProjectSampleFileName)
            .Content;

        Assert.Contains("SELECT Id, Name, Value, Category, HeavyComputation(Value) as Heavy", sample);
        Assert.Contains("WHERE Value > 100", sample);
        Assert.Contains("SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows", sample);
        Assert.Contains("CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]", sample);
        Assert.Contains("ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (ko3iko.Value > 100); threshold 4096", sample);
        Assert.Contains("EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>", sample);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>", sample);
        Assert.Contains("QueryRows.FromRowShards(", sample);
        Assert.Contains("new ResultRow0(ko3iko.Id, ko3iko.Name, ko3iko.Value, ko3iko.Category", sample);
        Assert.Contains("__resultRuntimeV2RegressionLibrary0.HeavyComputation(ko3iko.Value)", sample);
        Assert.IsFalse(sample.Contains("__musoqFinalShapeRows.Add(__musoqProjectedShape);", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("GetOrAddCachedMethod", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentDictionary", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.Add(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.AddUnchecked(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Execution IR does not support this query shape", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TableRowSource", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2LexerManyColumnsSample_WhenCheckedIn_ShouldStayOnSimpleDirectProjectionPath()
    {
        var sample = ReadSample(RuntimeV2LexerManyColumnsSampleFileName)
            .Content;

        Assert.Contains("SELECT Id as C01", sample);
        Assert.Contains("CASE WHEN Value > 100 THEN 'High' ELSE 'Low' END as C49", sample);
        Assert.Contains("CASE WHEN Salary > 1000 THEN 'Large' ELSE 'Small' END as C50", sample);
        Assert.Contains("SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows", sample);
        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", sample);
        Assert.Contains("private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(", sample);
        Assert.Contains("yield return new ResultShape0(", sample);
        Assert.Contains("yield return new ResultRow0(__musoqShapeRow.C01", sample);
        Assert.DoesNotContain("private Table ComputeTable_compiled_0(", sample);
        Assert.IsFalse(sample.Contains(ParallelFilterProjectLoopPattern, StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TableRowSource", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.Contains("private const string __columnIndexPairs", sample);
        Assert.Contains("private static readonly Dictionary<string, int> __columnIndexes = CreateColumnIndexes();", sample);
        Assert.Contains("private static Dictionary<string, int> CreateColumnIndexes()", sample);
        Assert.Contains("public override bool HasColumn(string name) => __columnIndexes.ContainsKey(name);", sample);
        Assert.Contains("__columnIndexes.TryGetValue(name, out var columnIndex)", sample);
        Assert.Contains("public override object this[string name]", sample);
        Assert.Contains("private static readonly Action<ResultRow0, object>[] __assigners", sample);
        Assert.IsFalse(sample.Contains("private static int GetColumnIndex(string name) => name switch", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("GetColumnIndex(name) >= 0", StringComparison.Ordinal));
        Assert.IsLessThanOrEqualTo(740, CountLines(sample), RuntimeV2LexerManyColumnsSampleFileName);
    }

    [TestMethod]
    public void RuntimeV2DecimalConversionSample_WhenCheckedIn_ShouldUseTypedDecimalComparison()
    {
        var sample = ReadSample(RuntimeV2DecimalConversionSampleFileName)
            .Content;

        Assert.Contains("WHERE TryConvertToDecimalComparison(Amount) > 100.50d", sample);
        Assert.Contains("SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows", sample);
        Assert.Contains("ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (TryConvertToDecimalComparison(ko3iko.Amount) > 100,50); threshold 4096", sample);
        Assert.Contains("Let [tryConvertToDecimalComparison: decimal? = TryConvertToDecimalComparison(amount)]", sample);
        Assert.Contains("decimal? tryConvertToDecimalComparison = (decimal?)__resultLibraryBase0.TryConvertToDecimalComparison(amount);", sample);
        Assert.Contains("if ((Operators.SqlCompare<decimal?, decimal>(tryConvertToDecimalComparison, 100.50m", sample);
        Assert.Contains("new ResultRow0(ko3iko.Id, tryConvertToDecimalComparison)", sample);
        Assert.Contains("QueryRows.FromRowShards(", sample);
        Assert.Contains("EvaluationHelper.ProjectChunkedRowsParallel<", sample);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<", sample);
        Assert.DoesNotContain("TableProjectionRows.ProjectOptionalRowsSerial<", sample);
        Assert.AreEqual(2, CountOccurrences(
            sample,
            "decimal? tryConvertToDecimalComparison = (decimal?)__resultLibraryBase0.TryConvertToDecimalComparison(amount);"));
        Assert.IsFalse(sample.Contains("TryConvertToDecimalComparison((object)", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Convert.ChangeType", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TableRowSource", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RuntimeV2CompositeRegressionCanarySample_WhenCheckedIn_ShouldUseAllRuntimeV2FastPaths()
    {
        var sample = ReadSample(RuntimeV2CompositeRegressionCanarySampleFileName)
            .Content;

        Assert.Contains("ExpensiveCompute(Value) as Computed", sample);
        Assert.Contains("WHERE Contains(Email, 'gmail')", sample);
        Assert.Contains("Sum(ToDecimal(Salary)) over (partition by Department order by Salary)", sample);
        Assert.Contains("Rank() over (partition by Department order by Salary desc)", sample);
        Assert.Contains("ORDER BY Department, Salary desc, Computed desc", sample);
        var libraryTargetMatch = Regex.Match(
            sample,
            @"CreateObject \[(?<name>__[A-Za-z0-9_]*RuntimeV2RegressionLibrary0): RuntimeV2RegressionLibrary\]");
        Assert.IsTrue(libraryTargetMatch.Success, sample);
        var libraryTarget = libraryTargetMatch.Groups["name"].Value;
        Assert.Contains("ko3iko.Email.Contains(\"gmail\", StringComparison.OrdinalIgnoreCase)", sample);
        Assert.Contains("ko3iko.FirstName.StartsWith(\"A\", StringComparison.OrdinalIgnoreCase)", sample);
        Assert.Contains("var resultSums0Value = ((decimal?)ko3iko.Salary);", sample);
        Assert.Contains($"{libraryTarget}.ExpensiveCompute(ko3iko.Value)", sample);
        Assert.Contains("ComputeSumWindowKernel[BoundedRows]", sample);
        Assert.Contains("ComputeRankWindow", sample);
        Assert.Contains("var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];", sample);
        Assert.Contains("WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys", sample);
        Assert.Contains("var resultSums0PrefixSum = System.Buffers.ArrayPool<decimal>.Shared.Rent", sample);
        Assert.Contains("var resultRanks1OrderKeys = new WindowResultRanks1OrderKeysKey[resultWindowRows.Count];", sample);
        Assert.Contains("var resultRanks1SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultSums0Partitions, resultRanks1OrderKeys, false);", sample);
        Assert.Contains("long resultRanks1Rank = 1L;", sample);
        Assert.Contains("TopOffsetShapeRows [result -> resultTopOffset by Department ASC, Salary DESC, Computed DESC, skip 10, take 20, BoundedHeap]", sample);
        Assert.Contains("var result = new List<ResultShape0>(resultWindowRows.Count);", sample);
        Assert.Contains("EvaluationHelper.SelectTopOffsetRecords(result, 10, 20, Comparer<ResultShape0>.Create", sample);
        Assert.IsFalse(sample.Contains("private sealed class ResultRow0OrderBy_1A_2D_3DComparer : IComparer<ResultRow0>", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("CreateObject [__resultLibraryBase", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase()", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase().Contains", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase().StartsWith", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Plugins.LibraryBase().ToDecimal", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionLibrary().ExpensiveCompute", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultSums0IntOrderBuilder", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultRanks1IntOrderBuilder", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeRank", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("AppendTopOffsetRowsDirect", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("RowOrderKey", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("row[", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ConcurrentQueue", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TableRowSource", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetColumnValue(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("0.0 us", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldUseGeneratedRowsWithoutObjectsRowValueArrays()
    {
        var failures = ReadAllSamples()
            .Select(sample =>
            {
                var stagingLocals = GeneratedRowStagingLocalPattern
                    .Matches(sample.Content)
                    .Cast<Match>()
                    .Select(static match => match.Value)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                var hasObjectsRowValueArray = sample.Content.Contains(
                    ObjectsRowValueArrayCreationPattern,
                    StringComparison.Ordinal);

                return new
                {
                    sample.FileName,
                    StagingLocals = stagingLocals,
                    HasObjectsRowValueArray = hasObjectsRowValueArray
                };
            })
            .Where(static sample => sample.StagingLocals.Length > 0 || sample.HasObjectsRowValueArray)
            .Select(static sample =>
            {
                var reasons = new List<string>();

                if (sample.StagingLocals.Length > 0)
                    reasons.Add($"staging locals: {string.Join(", ", sample.StagingLocals)}");

                if (sample.HasObjectsRowValueArray)
                    reasons.Add("ObjectsRow value-array creation");

                return $"{sample.FileName}: {string.Join("; ", reasons)}";
            })
            .ToArray();

        Assert.IsEmpty(
            failures,
            $"Generated samples still allocate staging row/value-array objects: {string.Join("; ", failures)}");
    }

    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldNotEmitLocalHelperFunctionsOrNullableCastWhitespace()
    {
        var failures = ReadAllSamples()
            .Select(sample =>
            {
                var reasons = new List<string>();

                if (GeneratedLocalFunctionPattern.IsMatch(sample.Content))
                    reasons.Add("local helper function");

                if (sample.Content.Contains("? )", StringComparison.Ordinal))
                    reasons.Add("nullable cast/type whitespace");

                return new
                {
                    sample.FileName,
                    Reasons = reasons
                };
            })
            .Where(static sample => sample.Reasons.Count > 0)
            .Select(static sample => $"{sample.FileName}: {string.Join(", ", sample.Reasons)}")
            .ToArray();

        Assert.IsEmpty(failures, $"Generated samples contain non-maintainable helper shapes: {string.Join("; ", failures)}");
    }

    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldNotEmitRetiredAggregateAndRowPatterns()
    {
        var samples = ReadAllSamples().ToArray();
        var failures = RetiredGeneratedCodePatterns
            .Select(budget => new
            {
                budget.Pattern,
                budget.Budget,
                Actual = CountOccurrences(samples, budget.Pattern)
            })
            .Where(static budget => budget.Actual > budget.Budget)
            .Select(static budget => $"{budget.Pattern}: {budget.Actual}/{budget.Budget}")
            .ToArray();

        Assert.IsEmpty(
            failures,
            "Generated samples still contain retired runtime-v2 aggregate/generated-row patterns: " + string.Join(", ", failures));
    }

    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldNotUseInlineGeneratedRowCasts()
    {
        var occurrences = ReadAllSamples()
            .SelectMany(CreateGeneratedRowCastOccurrences)
            .Where(static occurrence =>
                IntentionalGeneratedRowCastSampleFileNames.Contains(occurrence.FileName, StringComparer.Ordinal) is false)
            .ToArray();
        var failures = occurrences
            .Select(static occurrence => $"{occurrence.FileName}: {occurrence.Line} ({occurrence.Category})")
            .ToArray();

        Assert.IsEmpty(
            failures,
            "Generated-row casts must use typed locals, typed buffers, or centralized helpers such as EvaluationHelper.CastGeneratedRows<T>: " + string.Join("; ", failures));
    }

    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldNotUseTableBackedStoredRowsInGeneratedCode()
    {
        var failures = ReadAllSamples()
            .Select(sample =>
            {
                var generatedCode = ExtractGeneratedCodeSection(sample.Content);
                var reasons = new List<string>();

                if (generatedCode.Contains("_tableResults[", StringComparison.Ordinal) &&
                    IntentionalTableBackedStoredRowSampleFileNames.Contains(sample.FileName, StringComparer.Ordinal) is false)
                    reasons.Add("_tableResults slot access");

                if (Regex.IsMatch(
                    generatedCode,
                    @"private static (?:Musoq\.Evaluator\.Tables\.)?Table BuildCte",
                    RegexOptions.CultureInvariant))
                {
                    reasons.Add("table-returning CTE helper");
                }

                if (generatedCode.Contains("CreateAsOfIndex<Row", StringComparison.Ordinal) ||
                    generatedCode.Contains("CreateAsOfIndex<Musoq.Evaluator.Tables.Row", StringComparison.Ordinal))
                {
                    reasons.Add("row-backed ASOF CTE index");
                }

                return new
                {
                    sample.FileName,
                    Reasons = reasons
                };
            })
            .Where(static sample => sample.Reasons.Count > 0)
            .Select(static sample => $"{sample.FileName}: {string.Join(", ", sample.Reasons)}")
            .ToArray();

        Assert.IsEmpty(
            failures,
            "Generated samples still contain table-backed stored-row drift: " + string.Join("; ", failures));
    }
}
