using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static void AssertTypedWindowSourceBufferSample(
        IReadOnlyDictionary<string, GeneratedCodeSampleFile> samples,
        string fileName)
    {
        var sample = samples[fileName].Content;

        Assert.Contains(
            "var windowSourceTable = new List<WindowSourceRow0>();",
            sample);
        Assert.Contains(
            "var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<WindowSourceRow0>(windowSourceTable);",
            sample);
        Assert.IsFalse(
            sample.Contains("new Table(\"windowSourceTable\"", StringComparison.Ordinal),
            fileName);
        Assert.IsFalse(
            sample.Contains("windowSourceTable.Rows", StringComparison.Ordinal),
            fileName);
        Assert.AreEqual(
            2,
            CountOccurrences(sample, "WindowSourceRow0 windowSource = resultWindowRows[windowIndex];"),
            fileName);
        Assert.IsFalse(
            sample.Contains("Musoq.Evaluator.Tables.Row windowSource = resultWindowRows[windowIndex];", StringComparison.Ordinal),
            fileName);
        Assert.IsFalse(
            sample.Contains("((WindowSourceRow0)windowSource)", StringComparison.Ordinal),
            fileName);
    }

    private static void AssertTypedApplyWindowRowsSample(GeneratedCodeSampleFile sample)
    {
        Assert.Contains(
            "var apply_0_i_n_mTable = new List<apply_0_i_n_mRow0>();",
            sample.Content,
            sample.FileName);
        Assert.Contains(
            "var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<apply_0_i_n_mRow0>(apply_0_i_n_mTable);",
            sample.Content,
            sample.FileName);
        Assert.Contains(
            "List<apply_0_i_n_mRow0> apply_0_i_n_mTable)",
            sample.Content,
            sample.FileName);
        Assert.Contains(
            "private static void ExtractResultRowNumbersWindowKeys(IReadOnlyList<apply_0_i_n_mRow0> resultWindowRows",
            sample.Content,
            sample.FileName);
        Assert.Contains(
            "var result = new List<ResultShape0>(resultWindowRows.Count);",
            sample.Content,
            sample.FileName);
        Assert.Contains(
            "apply_0_i_n_mRow0 apply_0_i_n_m = resultWindowRows[windowIndex];",
            sample.Content,
            sample.FileName);
        Assert.IsFalse(
            sample.Content.Contains("IReadOnlyList<Musoq.Evaluator.Tables.Row> resultWindowRows", StringComparison.Ordinal),
            sample.FileName);
        Assert.IsFalse(
            sample.Content.Contains("new Table(\"apply_0_i_n_mTable\"", StringComparison.Ordinal),
            sample.FileName);
        Assert.IsFalse(
            sample.Content.Contains("apply_0_i_n_mTable.Rows", StringComparison.Ordinal),
            sample.FileName);
        Assert.IsFalse(
            sample.Content.Contains("apply_0_i_n_mTable.AddDirect", StringComparison.Ordinal),
            sample.FileName);
        Assert.IsFalse(
            sample.Content.Contains("((apply_0_i_n_mRow0)", StringComparison.Ordinal),
            sample.FileName);
    }

    private static void AssertGroupedWindowHelperShape(
        GeneratedCodeSampleFile sample,
        bool expectAggregateHelpers)
    {
        var computeMethod = GetComputeMethod(sample.Content);

        Assert.Contains("ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);", computeMethod, sample.FileName);
        Assert.Contains("var result = new List<ResultShape0>(resultWindowRows.Count);", computeMethod, sample.FileName);
        Assert.Contains("var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create", computeMethod, sample.FileName);
        Assert.Contains("__musoqFinalShapeRows.Add(resultSortedRowsRow);", computeMethod, sample.FileName);
        Assert.Contains("for (int windowIndex", computeMethod, sample.FileName);
        Assert.Contains("private static void ExtractResultRowNumbersWindowKeys", sample.Content, sample.FileName);
        Assert.IsFalse(sample.Content.Contains("private static void AppendResultWindowRows", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("private static Musoq.Evaluator.Tables.Table BuildResultSorted", StringComparison.Ordinal), sample.FileName);

        if (!expectAggregateHelpers)
        {
            Assert.Contains("_cteRowResults.Slot0 = BuildCte0", computeMethod, sample.FileName);
            Assert.IsFalse(computeMethod.Contains("_tableResults[0] = BuildCte0", StringComparison.Ordinal), sample.FileName);
            return;
        }

        Assert.Contains("PopulateWindowSourceTableSingleKeyGroups(windowSourceTable_iRows", computeMethod, sample.FileName);
        Assert.Contains("FinalizeWindowSourceTableSingleKeyGroups(windowSourceTable, groupsToFinalize, token);", computeMethod, sample.FileName);
        Assert.IsFalse(computeMethod.Contains("foreach (var i in windowSourceTable_iRows)", StringComparison.Ordinal), sample.FileName);
        Assert.Contains("private static void PopulateWindowSourceTableSingleKeyGroups", sample.Content, sample.FileName);
        Assert.Contains("ref WindowSourceTableAggregateGroup nullGroup, CancellationToken token)", sample.Content, sample.FileName);
        Assert.Contains("private static void FinalizeWindowSourceTableSingleKeyGroups", sample.Content, sample.FileName);
        Assert.Contains("List<WindowSourceRow0> windowSourceTable, List<WindowSourceTableAggregateGroup> groupsToFinalize, CancellationToken token)", sample.Content, sample.FileName);
        Assert.IsFalse(sample.Content.Contains("Musoq.Evaluator.Tables.Table windowSourceTable", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertTypedCteHashJoinSample(GeneratedCodeSampleFile sample, string rowTypeName)
    {
        var generatedCode = ExtractGeneratedCodeSection(sample.Content);

        Assert.Contains($"Dictionary<string, HashJoinBucket<{rowTypeName}>>", generatedCode, sample.FileName);
        Assert.Contains($"{rowTypeName} ", generatedCode, sample.FileName);
        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", generatedCode, sample.FileName);
        Assert.Contains($"{rowTypeName} c = __storedTable0Rows[__storedTable0Index];", generatedCode, sample.FileName);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(", generatedCode, sample.FileName);
        Assert.Contains($"HashJoinBucket<{rowTypeName}>", sample.Content, sample.FileName);
        Assert.IsFalse(
            generatedCode.Contains($"EvaluationHelper.CastGeneratedRows<{rowTypeName}>", StringComparison.Ordinal),
            sample.FileName);
        Assert.IsFalse(generatedCode.Contains($"(({rowTypeName})", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertTypedCteSidecarHashJoinSample(GeneratedCodeSampleFile sample, string payloadTypeName)
    {
        var generatedCode = ExtractGeneratedCodeSection(sample.Content);

        Assert.Contains($"Dictionary<string, HashJoinBucket<{payloadTypeName}>>", generatedCode, sample.FileName);
        Assert.Contains($"HashJoinBucket<{payloadTypeName}>", sample.Content, sample.FileName);
        Assert.Contains("var cHash = _cteIndexResults.Slot0;", generatedCode, sample.FileName);
        Assert.Contains($"{payloadTypeName} cte0SidecarPayload0", generatedCode, sample.FileName);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(", generatedCode, sample.FileName);
        Assert.IsFalse(generatedCode.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(generatedCode.Contains("_tableResults[0]", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(generatedCode.Contains("EvaluationHelper.CastGeneratedRows<Cte0Row0>", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(generatedCode.Contains("private sealed class Cte0Row0", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertTypedCteKeySetSample(GeneratedCodeSampleFile sample, string rowTypeName)
    {
        var generatedCode = ExtractGeneratedCodeSection(sample.Content);

        Assert.Contains("HashSet<string>", generatedCode, sample.FileName);
        Assert.Contains($"{rowTypeName} ", generatedCode, sample.FileName);
        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", generatedCode, sample.FileName);
        Assert.Contains("HashSet<string>", sample.Content, sample.FileName);
        Assert.IsFalse(
            generatedCode.Contains($"EvaluationHelper.CastGeneratedRows<{rowTypeName}>", StringComparison.Ordinal),
            sample.FileName);
        Assert.IsFalse(generatedCode.Contains($"(({rowTypeName})", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(generatedCode.Contains($"HashJoinBucket<{rowTypeName}>", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertTypedCteAsOfSample(GeneratedCodeSampleFile sample)
    {
        Assert.Contains(
            "EvaluationHelper.CreateAsOfIndex<Cte0Row0, decimal>(_cteRowResults.Slot0",
            sample.Content,
            sample.FileName);
        Assert.IsFalse(sample.Content.Contains("EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tables.Row>", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("((Cte0Row0)", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertTypedCteAggregateHashJoinSample(GeneratedCodeSampleFile sample)
    {
        var generatedCode = ExtractGeneratedCodeSection(sample.Content);

        Assert.Contains("Dictionary<string, HashJoinBucket<Cte1HashPayload0>>", sample.Content, sample.FileName);
        Assert.Contains("List<Cte0Row0> BuildCteLevel0Task0", sample.Content, sample.FileName);
        Assert.Contains("object BuildCteLevel0Task1", sample.Content, sample.FileName);
        Assert.Contains("LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]", sample.Content, sample.FileName);
        Assert.Contains(
            "UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, r, l);",
            generatedCode,
            sample.FileName);
        Assert.Contains("var rHash = _cteIndexResults.Slot0;", generatedCode, sample.FileName);
        Assert.Contains("Cte1HashPayload0 r", generatedCode, sample.FileName);
        Assert.IsFalse(
            generatedCode.Contains("EvaluationHelper.CastGeneratedRows<Cte1Row0>(_tableResults[1].Rows)", StringComparison.Ordinal),
            sample.FileName);
        Assert.IsFalse(generatedCode.Contains("var __storedTable1Rows = _cteRowResults.Slot1;", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(generatedCode.Contains("_tableResults[1]", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(generatedCode.Contains("((Cte0Row0)", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(generatedCode.Contains("((Cte1Row0)", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertTypedRepeatedCteSelfJoinSample(GeneratedCodeSampleFile sample)
    {
        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", sample.Content, sample.FileName);
        Assert.Contains("Dictionary<string, HashJoinBucket<Cte0HashPayload0>>", sample.Content, sample.FileName);
        Assert.Contains("LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]", sample.Content, sample.FileName);
        Assert.IsFalse(sample.Content.Contains("var __storedTable0Rows = _tableResults[0].Rows;", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("((Cte0Row0)", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertDynamicCteAsOfTypedSample(GeneratedCodeSampleFile sample)
    {
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<Cte0Row0, int>(_cteRowResults.Slot0", sample.Content, sample.FileName);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(l.Name, r.Name));", sample.Content, sample.FileName);
        Assert.IsFalse(sample.Content.Contains("_tableResults[0]", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("new Table(\"cte0\"", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("EvaluationHelper.CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows)", StringComparison.Ordinal), sample.FileName);
    }

    private static void AssertOperatorFamilyBudgets(IReadOnlyList<GeneratedCodeSampleFile> samples)
    {
        foreach (var sampleGroup in samples.GroupBy(static sample => sample.Category))
        {
            var budget = OperatorFamilyBudgets[sampleGroup.Key];
            var actual = CountShapes(sampleGroup);

            AssertShapeBudget($"generated-code {sampleGroup.Key} samples", budget, actual);
        }
    }

    private static void AssertShapeBudget(string scope, ShapeBudget budget, ShapeBudget actual)
    {
        var offenders = ShapeBudgetEntries
            .Select(entry => new ShapeBudgetCheck(
                entry.Pattern,
                entry.GetCount(budget),
                entry.GetCount(actual)))
            .Where(static shapeBudget => shapeBudget.Actual > shapeBudget.Budget)
            .Select(static shapeBudget => $"{shapeBudget.Pattern}: {shapeBudget.Actual}/{shapeBudget.Budget}")
            .ToArray();

        Assert.IsEmpty(offenders, $"{scope} exceeded shape budgets: {string.Join(", ", offenders)}");
    }

    private static void AssertStreamingWindowUsesDirectNumericConversion(
        string fileName,
        string content,
        string resultName)
    {
        Assert.Contains(
            $"{resultName}Function.Accumulate((decimal?)ko3iko.Population);",
            content,
            $"{fileName}: window value conversion should use direct numeric conversion");
        Assert.IsFalse(
            content.Contains($"{resultName}LibraryBase0", StringComparison.Ordinal),
            $"{fileName}: numeric window value conversion should not allocate LibraryBase");
        Assert.IsFalse(
            content.Contains("new Musoq.Plugins.LibraryBase().ToDecimal", StringComparison.Ordinal),
            $"{fileName}: window value conversion still allocates LibraryBase inside the row loop");
        Assert.IsFalse(
            content.Contains($"{resultName}Values", StringComparison.Ordinal),
            $"{fileName}: streaming plugin window should not extract an object[] value buffer");
        Assert.IsFalse(
            content.Contains($"{resultName}Function.SetArguments(Array.Empty<object?>())", StringComparison.Ordinal),
            $"{fileName}: no-arg streaming plugin window should not call object argument dispatch");
        Assert.IsFalse(
            content.Contains($"{resultName}Function.AccumulateValue", StringComparison.Ordinal),
            $"{fileName}: streaming plugin window should not call boxed accumulation");
        Assert.IsFalse(
            content.Contains($"{resultName}Function.GetCurrentValue", StringComparison.Ordinal),
            $"{fileName}: streaming plugin window should not call boxed result access");
        Assert.IsFalse(
            content.Contains("WindowFunctionHelpers.ComputeTypedPluginWindowFunction", StringComparison.Ordinal),
            $"{fileName}: streaming plugin window should not fall back to the generic helper");
    }

    private static void AssertWindowAggregateKernelUsesDirectNumericConversion(
        string fileName,
        string content,
        string resultName,
        string kernelName)
    {
        Assert.Contains(kernelName, content);
        Assert.Contains(
            $"var {resultName}Value = ((decimal?)ko3iko.Population);",
            content,
            $"{fileName}: window value conversion should use direct numeric conversion");
        Assert.IsFalse(
            content.Contains($"{resultName}Function", StringComparison.Ordinal),
            $"{fileName}: aggregate kernel should not instantiate the plugin window function");
        Assert.IsFalse(
            content.Contains("SetArguments", StringComparison.Ordinal),
            $"{fileName}: aggregate kernel should not call the plugin argument path");
        Assert.IsFalse(
            content.Contains($"{resultName}LibraryBase0", StringComparison.Ordinal),
            $"{fileName}: numeric window value conversion should not allocate LibraryBase");
        Assert.IsFalse(
            content.Contains("new Musoq.Plugins.LibraryBase().ToDecimal", StringComparison.Ordinal),
            $"{fileName}: window value conversion still allocates LibraryBase inside the row loop");
        Assert.IsFalse(
            content.Contains($"{resultName}Values", StringComparison.Ordinal),
            $"{fileName}: aggregate kernel should not extract an object[] value buffer");
    }

}
