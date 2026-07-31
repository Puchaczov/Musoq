using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldContainExpectedFileCount()
    {
        Assert.HasCount(ExpectedSampleFileCount, ReadAllSamples());
    }

    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldUseTableRunnableContract()
    {
        var samples = ReadAllSamples();
        var legacyContractSamples = samples
            .Where(static sample => sample.Content.Contains("BaseOperations, IRunnable, IParameterizedRunnable", StringComparison.Ordinal))
            .Select(static sample => sample.FileName)
            .ToArray();
        var missingTableContractSamples = samples
            .Where(static sample =>
                !sample.Content.Contains("BaseOperations, ITableRunnable, IParameterizedRunnable", StringComparison.Ordinal) &&
                !sample.Content.Contains("BaseOperations, ITableRunnable, IContextTableRunnable, IParameterizedRunnable", StringComparison.Ordinal))
            .Select(static sample => sample.FileName)
            .ToArray();

        Assert.IsEmpty(
            legacyContractSamples,
            $"Generated samples should not declare IRunnable: {string.Join(", ", legacyContractSamples)}");
        Assert.IsEmpty(
            missingTableContractSamples,
            $"Generated samples should declare ITableRunnable: {string.Join(", ", missingTableContractSamples)}");
    }

    [TestMethod]
    public void InClauseSamples_WhenCheckedIn_ShouldStayWithinInlineArrayAllocationBudget()
    {
        var offenders = ReadAllSamples()
            .Where(static sample => ContainsInlineArrayIndexOf(sample.Content))
            .Select(static sample => sample.FileName)
            .ToArray();

        Assert.IsLessThanOrEqualTo(
            InlineInArrayAllocationBudget,
            offenders.Length,
            $"Inline per-row IN array allocations exceeded budget. Offenders: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void LargeInClauseSample_WhenCheckedIn_ShouldUseSwitchExpression()
    {
        var sample = ReadSample(LargeInClauseSampleFileName).Content;

        Assert.Contains("name switch", sample);
        Assert.Contains("\"A\" or \"B\"", sample);
        Assert.IsFalse(sample.Contains("__inSet_", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains(HashSetPattern, StringComparison.Ordinal));
        Assert.IsFalse(ContainsInlineArrayIndexOf(sample));
    }

    [TestMethod]
    public void LargeInClauseSample_WhenCheckedIn_ShouldUseStaticColumnMetadata()
    {
        var sample = ReadSample(LargeInClauseSampleFileName).Content;

        Assert.Contains(StaticColumnMetadataPattern, sample);
        Assert.Contains(StaticSchemaMetadataPattern, sample);
        Assert.IsFalse(sample.Contains("var ko3ikoInferredInfoTable = new ISchemaColumn[]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Table(\"result\", new Column[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SerialOutputSamples_WhenCheckedIn_ShouldUseShapeStreamAndCapacityHints()
    {
        var samples = ReadNamedSamples("Q01_SimpleSelectWhere.cs", "Q07_GroupByHavingOrderBy.cs")
            .ToDictionary(static sample => sample.FileName);
        var simpleSelect = samples["Q01_SimpleSelectWhere.cs"].Content;
        var groupedSort = samples["Q07_GroupByHavingOrderBy.cs"].Content;

        Assert.Contains("private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(", simpleSelect);
        Assert.Contains("yield return new ResultShape0(ko3iko.Name, population);", simpleSelect);
        Assert.Contains("yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Population);", simpleSelect);
        Assert.DoesNotContain("private Table ComputeTable_compiled_0(", simpleSelect);
        Assert.Contains("result.Add(new ResultShape0(", groupedSort);
        Assert.Contains(EnsureCapacityPattern, groupedSort);
    }

    [TestMethod]
    public void SourceScans_WhenCheckedIn_ShouldSplitDirectRowSourceRows()
    {
        var samples = ReadNamedSamples(
                "Q01_SimpleSelectWhere.cs",
                InnerJoinSampleFileName)
            .ToDictionary(static sample => sample.FileName);
        var inlineRowSourceReads = samples.Values
            .SelectMany(static sample => sample.Content
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Where(static line =>
                    line.Contains(".GetRowSource<", StringComparison.Ordinal) &&
                    line.Contains(").Chunks;", StringComparison.Ordinal))
                .Select(line => $"{sample.FileName}: {line.Trim()}"))
            .ToArray();

        Assert.IsEmpty(
            inlineRowSourceReads,
            $"Direct source scans should split GetRowSource<T>() from .Chunks: {string.Join(Environment.NewLine, inlineRowSourceReads)}");

        var simpleSelect = samples["Q01_SimpleSelectWhere.cs"].Content;
        Assert.Contains("var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource", simpleSelect);
        Assert.Contains("var ko3ikoRows = ko3ikoRowsSource.Chunks;", simpleSelect);

        var innerJoin = samples["Q03_InnerJoin.cs"].Content;
        Assert.Contains("var aRowsSource = __aSchema.GetRowSource", innerJoin);
        Assert.Contains("var aRows = aRowsSource.Chunks;", innerJoin);
        Assert.Contains("var bRowsSource = __bSchema.GetRowSource", innerJoin);
        Assert.Contains("var bRows = bRowsSource.Chunks;", innerJoin);
    }

    [TestMethod]
    public void GeneratedClassMembers_WhenCheckedIn_ShouldKeepReadableOrderAndCallbackInlining()
    {
        const string aggressiveInliningAttribute =
            "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        var sample = ReadSample(InnerJoinSampleFileName).Content;

        AssertFragmentsInOrder(
            sample,
            "public event DataSourceEventHandler DataSourceProgress;",
            "public event QueryPhaseEventHandler PhaseChanged;",
            "public Table Run(CancellationToken token)",
            "private IEnumerable<ResultRow0> ComputeRows_compiled_0(",
            "private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(",
            "private void OnDataSourceProgress",
            "private void OnPhaseChanged",
            "private sealed class ResultRow0",
            "private sealed class ResultShape0");
        Assert.IsTrue(
            Regex.IsMatch(
                sample,
                Regex.Escape(aggressiveInliningAttribute) + @"\s+private void OnDataSourceProgress"),
            "OnDataSourceProgress should be aggressively inlined.");
        Assert.IsTrue(
            Regex.IsMatch(
                sample,
                Regex.Escape(aggressiveInliningAttribute) + @"\s+private void OnPhaseChanged"),
            "OnPhaseChanged should be aggressively inlined.");
    }

    [TestMethod]
    public void ScriptParameterSamples_WhenCheckedIn_ShouldBindOnceAndUseTypedLocals()
    {
        var samples = ReadNamedSamples(
                ScriptParametersWhereSelectSampleFileName,
                ScriptParameterPrimitiveDefaultsSampleFileName,
                ScriptParameterSourceArgumentSampleFileName,
                ScriptParameterTypedComparisonSampleFileName,
                ScriptParameterNumericWideningComparisonSampleFileName)
            .ToDictionary(static sample => sample.FileName);
        var whereSelect = samples[ScriptParametersWhereSelectSampleFileName].Content;
        var primitiveDefaults = samples[ScriptParameterPrimitiveDefaultsSampleFileName].Content;
        var sourceArgument = samples[ScriptParameterSourceArgumentSampleFileName].Content;
        var typedComparison = samples[ScriptParameterTypedComparisonSampleFileName].Content;
        var numericWidening = samples[ScriptParameterNumericWideningComparisonSampleFileName].Content;

        Assert.Contains(
            "var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"country\");",
            whereSelect);
        Assert.Contains(
            "var paramMinPopulation = ScriptParameterBinder.GetOptional<int>(__musoqExecutionState.Parameters, \"minPopulation\", 100);",
            whereSelect);
        Assert.AreEqual(2, CountOccurrences(whereSelect, "ScriptParameterBinder.Get"));
        Assert.Contains("if (((ko3iko.Country == paramCountry) && (population > paramMinPopulation)))", whereSelect);
        Assert.Contains("yield return new ResultShape0(ko3iko.Name, population, paramCountry);", whereSelect);
        Assert.Contains("yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Population, __musoqShapeRow.RequestedCountry);", whereSelect);

        Assert.Contains("var paramFlag = ScriptParameterBinder.GetOptional<bool>(__musoqExecutionState.Parameters, \"flag\", true);", primitiveDefaults);
        Assert.Contains("var paramCode = ScriptParameterBinder.GetOptional<char>(__musoqExecutionState.Parameters, \"code\", 'x');", primitiveDefaults);
        Assert.Contains(
            "var paramLimit = ScriptParameterBinder.GetOptional<int?>(__musoqExecutionState.Parameters, \"limit\", default(int?));",
            primitiveDefaults);
        Assert.Contains(
            "var paramId = ScriptParameterBinder.GetOptional<Guid>(__musoqExecutionState.Parameters, \"id\", new Guid(\"2ffcf6fa-3369-4300-946a-bb131a037985\"));",
            primitiveDefaults);
        Assert.Contains(
            "var paramCreated = ScriptParameterBinder.GetOptional<DateTime>(__musoqExecutionState.Parameters, \"created\", new DateTime(638397614450000000L, DateTimeKind.Utc));",
            primitiveDefaults);
        Assert.AreEqual(5, CountOccurrences(primitiveDefaults, "ScriptParameterBinder.Get"));
        Assert.Contains(
            "new ResultRow0(paramFlag, paramCode, paramLimit, paramId, paramCreated)",
            primitiveDefaults);

        Assert.Contains("var paramKey = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState.Parameters, \"key\", \"KEY_1\");", sourceArgument);
        Assert.AreEqual(1, CountOccurrences(sourceArgument, "ScriptParameterBinder.Get"));
        Assert.Contains("new object[] { paramKey }", sourceArgument);
        var parameterBindingIndex = sourceArgument.IndexOf("var paramKey = ", StringComparison.Ordinal);
        var sourceRowsIndex = sourceArgument.IndexOf("var __ko3ikoSchema = ", StringComparison.Ordinal);
        Assert.IsLessThan(sourceRowsIndex, parameterBindingIndex);

        Assert.Contains("var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"country\");", typedComparison);
        Assert.AreEqual(1, CountOccurrences(typedComparison, "ScriptParameterBinder.Get"));
        Assert.Contains("if ((country == paramCountry))", typedComparison);
        Assert.DoesNotContain("TryConvertTo", typedComparison);

        Assert.Contains(
            "var paramMinPopulation = ScriptParameterBinder.GetRequired<int>(__musoqExecutionState.Parameters, \"minPopulation\");",
            numericWidening);
        Assert.AreEqual(1, CountOccurrences(numericWidening, "ScriptParameterBinder.Get"));
        Assert.Contains("if ((population >= paramMinPopulation))", numericWidening);
        Assert.DoesNotContain("TryConvertTo", numericWidening);
        AssertTopLevelBindingBefore(
            ScriptParameterNumericWideningComparisonSampleFileName,
            numericWidening,
            "var paramMinPopulation = ",
            "var __ko3ikoSchema = ");
    }

    [TestMethod]
    public void ScriptParameterHelperCaptureSamples_WhenCheckedIn_ShouldPassTypedLocalsToHelpers()
    {
        var samples = ReadNamedSamples(
                ScriptParameterGroupByHelperCaptureSampleFileName,
                ScriptParameterJoinHelperCaptureSampleFileName,
                ScriptParameterCteHelperCaptureSampleFileName,
                ScriptParameterWindowHelperCaptureSampleFileName,
                ScriptParameterParallelHelperCaptureSampleFileName)
            .ToDictionary(static sample => sample.FileName);
        var groupBy = samples[ScriptParameterGroupByHelperCaptureSampleFileName].Content;
        var join = samples[ScriptParameterJoinHelperCaptureSampleFileName].Content;
        var cte = samples[ScriptParameterCteHelperCaptureSampleFileName].Content;
        var window = samples[ScriptParameterWindowHelperCaptureSampleFileName].Content;
        var parallel = samples[ScriptParameterParallelHelperCaptureSampleFileName].Content;

        AssertScriptParameterBinderCount(ScriptParameterGroupByHelperCaptureSampleFileName, groupBy, 2);
        Assert.Contains("ParallelSingleKeyAggregate_0(ko3ikoRows, 24, token, paramSuffix);", groupBy);
        Assert.DoesNotContain("SerialSingleKeyAggregate", groupBy);
        Assert.Contains("CancellationToken cancellationToken, string paramSuffix)", groupBy);
        Assert.Contains("private readonly string _paramSuffix;", groupBy);
        Assert.Contains("string groupKey = (ko3iko.Country + paramSuffix);", groupBy);
        AssertTopLevelBindingBefore(
            ScriptParameterGroupByHelperCaptureSampleFileName,
            groupBy,
            "var paramSuffix = ",
            "ParallelSingleKeyAggregate_0(");

        AssertScriptParameterBinderCount(ScriptParameterJoinHelperCaptureSampleFileName, join, 2);
        Assert.Contains("_cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults, paramSuffix);", join);
        Assert.Contains("CteRowResults _cteRowResults, string paramSuffix)", join);
        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", join);
        Assert.Contains("Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];", join);
        Assert.Contains("string key = (b.City + paramSuffix);", join);
        Assert.Contains("__resultLibraryBase0.Coalesce<string>((ab.b_Name + paramSuffix), paramFallback)", join);
        AssertTopLevelBindingBefore(
            ScriptParameterJoinHelperCaptureSampleFileName,
            join,
            "var paramSuffix = ",
            "_cteRowResults.Slot0 = BuildCte0(");

        AssertScriptParameterBinderCount(ScriptParameterCteHelperCaptureSampleFileName, cte, 1);
        Assert.Contains("_cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults, _cteIndexResults, paramCountry);", cte);
        Assert.Contains("CteRowResults _cteRowResults, CteIndexResults _cteIndexResults, string paramCountry)", cte);
        Assert.Contains("if ((ko3iko.Country == paramCountry))", cte);
        AssertTopLevelBindingBefore(
            ScriptParameterCteHelperCaptureSampleFileName,
            cte,
            "var paramCountry = ",
            "_cteRowResults.Slot0 = BuildCte0(");

        AssertScriptParameterBinderCount(ScriptParameterWindowHelperCaptureSampleFileName, window, 2);
        Assert.Contains("ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersPartitionKeys, resultRowNumbersOrderKeys, paramCountry, paramLabel);", window);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(ko3iko.Name, (long)resultRowNumbers[windowIndex], paramLabel));", window);
        Assert.IsFalse(window.Contains("AppendResultWindowRows(resultWindowRows, result, resultRowNumbers, paramLabel);", StringComparison.Ordinal));
        Assert.Contains("WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys, string paramCountry, string paramLabel)", window);
        Assert.Contains("resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey((ko3iko.Name + paramLabel));", window);
        AssertTopLevelBindingBefore(
            ScriptParameterWindowHelperCaptureSampleFileName,
            window,
            "var paramCountry = ",
            "ExtractResultRowNumbersWindowKeys(");

        AssertScriptParameterBinderCount(ScriptParameterParallelHelperCaptureSampleFileName, parallel, 2);
        Assert.Contains(ParallelFilterProjectLoopPattern, parallel);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>", parallel);
        Assert.Contains("QueryRows.FromRowShards(", parallel);
        Assert.Contains("(ko3iko) => (ko3iko.Value > paramThreshold)", parallel);
        Assert.Contains("new ResultRow0(ko3iko.Name, paramLabel, (int)__resultRuntimeV2RegressionLibrary0.HeavyComputation(ko3iko.Value))", parallel);
        AssertTopLevelBindingBefore(
            ScriptParameterParallelHelperCaptureSampleFileName,
            parallel,
            "var paramThreshold = ",
            "var __musoqTableParallelRows = ");

        foreach (var sample in new[]
                 {
                     samples[ScriptParameterGroupByHelperCaptureSampleFileName],
                     samples[ScriptParameterJoinHelperCaptureSampleFileName],
                     samples[ScriptParameterCteHelperCaptureSampleFileName],
                     samples[ScriptParameterWindowHelperCaptureSampleFileName],
                     samples[ScriptParameterParallelHelperCaptureSampleFileName]
                 })
        {
            AssertStaticHelpersDoNotReadRuntimeParameters(sample.FileName, sample.Content);
            AssertRowLoopsDoNotReadRuntimeParameters(sample.FileName, sample.Content);
        }
    }

    private static void AssertFragmentsInOrder(string value, params string[] fragments)
    {
        var previousIndex = -1;

        foreach (var fragment in fragments)
        {
            var index = value.IndexOf(fragment, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, index, $"Missing fragment '{fragment}'.");
            Assert.IsGreaterThan(previousIndex, index, $"Fragment '{fragment}' was not in the expected order.");
            previousIndex = index;
        }
    }

}
