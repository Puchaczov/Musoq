using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static string[] GetChainedApplyWindowShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(ChainedApplyWindowSampleFileName, content).ToList();

        AddChainedApplyLateMaterializationFailures(ChainedApplyWindowSampleFileName, content, failures);

        if (!content.Contains("EvaluationHelper.ConvertEnumerableOutputToChunks<int>", StringComparison.Ordinal))
            failures.Add($"{ChainedApplyWindowSampleFileName}: missing direct scalar chunk conversion");

        if (content.Contains("EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>", StringComparison.Ordinal))
            failures.Add($"{ChainedApplyWindowSampleFileName}: scalar enumerable still allocates PrimitiveTypeEntity wrappers");

        if (!ContainsGeneratedRowNumberKernel(content))
            failures.Add($"{ChainedApplyWindowSampleFileName}: missing row-number window computation");

        if (content.Contains("_tableResults[0] = statement0", StringComparison.Ordinal))
            failures.Add($"{ChainedApplyWindowSampleFileName}: still uses statement transition-table rewrite");

        return failures.ToArray();
    }

    private static string[] GetChainedApplyQualifyWindowShapeFailures(string content)
    {
        var failures = GetCommonChainedApplyQualifyWindowShapeFailures(
            ChainedApplyQualifyWindowSampleFileName,
            content,
            []);

        if (content.Contains("_tableResults[0] = statement0", StringComparison.Ordinal))
            failures.Add($"{ChainedApplyQualifyWindowSampleFileName}: still uses statement transition-table rewrite");

        return failures.ToArray();
    }

    private static string[] GetChainedApplyGroupedAggregateQualifyWindowShapeFailures(string content)
    {
        return GetCommonChainedApplyQualifyWindowShapeFailures(
            ChainedApplyGroupedAggregateQualifyWindowSampleFileName,
            content,
            ["SetAvg", "SetMin", "SetMax"]).ToArray();
    }

    private static List<string> GetCommonChainedApplyQualifyWindowShapeFailures(
        string fileName,
        string content,
        IReadOnlyList<string> aggregateSetMethods)
    {
        var failures = GetRetiredHelperShapeFailures(fileName, content).ToList();

        if (aggregateSetMethods.Count == 0)
            AddChainedApplyLateMaterializationFailures(fileName, content, failures);
        else
            AddChainedApplyAggregateStreamingFailures(fileName, content, failures);

        if (!ContainsGeneratedRowNumberKernel(content))
            failures.Add($"{fileName}: missing row-number window computation");

        if (!content.Contains("resultRowNumbers[windowIndex] <= 1", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing QUALIFY row-number guard");

        if (aggregateSetMethods.Count > 0)
            AddTypedAggregateStateFailures(fileName, content, aggregateSetMethods, failures);

        return failures;
    }

    private static string[] GetChainedApplyMixedDistinctAggregateSortShapeFailures(string content)
    {
        return GetChainedApplyMixedDistinctAggregateSortShapeFailures(
            ChainedApplyMixedDistinctAggregateSortSampleFileName,
            content,
            ["SetSum"],
            ["SumDistinct"]);
    }

    private static string[] GetChainedApplyMixedDistinctMinMaxAggregateSortShapeFailures(string content)
    {
        return GetChainedApplyMixedDistinctAggregateSortShapeFailures(
            ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName,
            content,
            ["SetMin", "SetMax"],
            ["MinDistinct", "MaxDistinct"]);
    }

    private static string[] GetChainedApplyMixedDistinctAvgAggregateSortShapeFailures(string content)
    {
        return GetChainedApplyMixedDistinctAggregateSortShapeFailures(
            ChainedApplyMixedDistinctAvgAggregateSortSampleFileName,
            content,
            ["SetAvg"],
            ["AvgDistinct"]);
    }

    private static string[] GetChainedApplyMixedDistinctMinMaxAggregateWindowShapeFailures(string content)
    {
        return GetChainedApplyMixedDistinctAggregateWindowShapeFailures(
            ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName,
            content,
            ["SetMin", "SetMax"],
            ["MinDistinct", "MaxDistinct"]);
    }

    private static string[] GetChainedApplyMixedDistinctAvgAggregateWindowShapeFailures(string content)
    {
        return GetChainedApplyMixedDistinctAggregateWindowShapeFailures(
            ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName,
            content,
            ["SetAvg"],
            ["AvgDistinct"]);
    }

    private static string[] GetChainedApplyMixedDistinctAggregateSortShapeFailures(
        string fileName,
        string content,
        IReadOnlyList<string> regularAggregateSetMethods,
        IReadOnlyList<string> distinctAggregateFinalizers)
    {
        var failures = GetRetiredHelperShapeFailures(fileName, content).ToList();
        AddChainedApplyAggregateStreamingFailures(fileName, content, failures);

        AddTypedAggregateStateFailures(
            fileName,
            content,
            [..regularAggregateSetMethods, "SetDistinctAggregate"],
            failures);

        foreach (var methodName in distinctAggregateFinalizers)
            if (!content.Contains($"{methodName}AggregateKernel<int>.Get", StringComparison.Ordinal))
                failures.Add($"{fileName}: missing {methodName} finalization");

        if (content.Contains("AggregateGroup : Group", StringComparison.Ordinal))
            failures.Add($"{fileName}: distinct aggregate group still inherits legacy Group");

        if (!content.Contains("resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing post-aggregate sort");

        if (content.Contains("_tableResults[0] = statement0", StringComparison.Ordinal))
            failures.Add($"{fileName}: still uses statement transition-table rewrite");

        return failures.ToArray();
    }

    private static string[] GetChainedApplyMixedDistinctAggregateWindowShapeFailures(
        string fileName,
        string content,
        IReadOnlyList<string> regularAggregateSetMethods,
        IReadOnlyList<string> distinctAggregateFinalizers)
    {
        var failures = GetRetiredHelperShapeFailures(fileName, content).ToList();
        AddChainedApplyAggregateStreamingFailures(fileName, content, failures);

        AddTypedAggregateStateFailures(
            fileName,
            content,
            [..regularAggregateSetMethods, "SetDistinctAggregate"],
            failures);

        foreach (var methodName in distinctAggregateFinalizers)
            if (!content.Contains($"{methodName}AggregateKernel<int>.Get", StringComparison.Ordinal))
                failures.Add($"{fileName}: missing {methodName} finalization");

        if (content.Contains("AggregateGroup : Group", StringComparison.Ordinal))
            failures.Add($"{fileName}: distinct aggregate group still inherits legacy Group");

        if (!ContainsGeneratedRowNumberKernel(content))
            failures.Add($"{fileName}: missing row-number window computation");

        if (content.Contains("_tableResults[0] = statement0", StringComparison.Ordinal))
            failures.Add($"{fileName}: still uses statement transition-table rewrite");

        return failures.ToArray();
    }

    private static void AddChainedApplyLateMaterializationFailures(
        string fileName,
        string content,
        List<string> failures)
    {
        if (content.Contains("var apply_0_i_nTable = new Table(\"apply_0_i_nTable\"", StringComparison.Ordinal))
            failures.Add($"{fileName}: still materializes first chained-apply transition table");

        if (content.Contains("foreach (var apply_0_i_n in apply_0_i_nTable.Rows)", StringComparison.Ordinal))
            failures.Add($"{fileName}: still reads second apply from first transition rows");

        if (content.Contains("var apply_0_i_n_mTable = new Table(\"apply_0_i_n_mTable\"", StringComparison.Ordinal))
            failures.Add($"{fileName}: still materializes final chained-apply window buffer as Table");

        if (!content.Contains("var apply_0_i_n_mTable = new List<apply_0_i_n_mRow0>();", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing typed final chained-apply window row buffer");

        if (!content.Contains("EvaluationHelper.MaterializeGeneratedRows<apply_0_i_n_mRow0>(apply_0_i_n_mTable)", StringComparison.Ordinal))
            failures.Add($"{fileName}: window rows are not materialized from the typed chained-apply buffer");

        if (content.Contains("EvaluationHelper.MaterializeGeneratedRows<apply_0_i_n_mRow0>(apply_0_i_n_mTable.Rows)", StringComparison.Ordinal))
            failures.Add($"{fileName}: still unwraps final chained-apply window rows through Table.Rows");

        if (content.Contains("apply_0_i_n_mTable.AddDirect", StringComparison.Ordinal))
            failures.Add($"{fileName}: still appends final chained-apply window rows through Table.AddDirect");

        if (!content.Contains("foreach (var nChunk in apply_0_i_n_mTable_nRows)", StringComparison.Ordinal) ||
            !content.Contains("foreach (var mChunk in apply_0_i_n_mTable_mRows)", StringComparison.Ordinal) ||
            !content.Contains("var n = nChunk[nIndex];", StringComparison.Ordinal) ||
            !content.Contains("var m = mChunk[mIndex];", StringComparison.Ordinal))
        {
            failures.Add($"{fileName}: missing nested direct apply loops");
        }
    }

    private static bool ContainsGeneratedRowNumberKernel(string content)
    {
        return content.Contains("resultRowNumbers[resultRowNumbersCurrentIndex] = resultRowNumbersPartitionIndex + 1L", StringComparison.Ordinal);
    }

    private static void AddChainedApplyAggregateStreamingFailures(
        string fileName,
        string content,
        List<string> failures)
    {
        if (content.Contains("var apply_0_i_nTable = new Table(\"apply_0_i_nTable\"", StringComparison.Ordinal))
            failures.Add($"{fileName}: still materializes first chained-apply transition table");

        if (content.Contains("foreach (var apply_0_i_n in apply_0_i_nTable.Rows)", StringComparison.Ordinal))
            failures.Add($"{fileName}: still reads second apply from first transition rows");

        if (content.Contains("var apply_0_i_n_mTable = new Table(\"apply_0_i_n_mTable\"", StringComparison.Ordinal))
            failures.Add($"{fileName}: still materializes final chained-apply table before grouping");

        if (content.Contains("foreach (var apply_0_i_n_m in apply_0_i_n_mTable.Rows)", StringComparison.Ordinal))
            failures.Add($"{fileName}: still aggregates by scanning the final chained-apply table");

        if (!content.Contains("foreach (var nChunk in ", StringComparison.Ordinal) ||
            !content.Contains("foreach (var mChunk in ", StringComparison.Ordinal) ||
            !content.Contains("var n = nChunk[nIndex];", StringComparison.Ordinal) ||
            !content.Contains("var m = mChunk[mIndex];", StringComparison.Ordinal))
        {
            failures.Add($"{fileName}: missing direct nested apply loops");
        }
    }

    private static void AddTypedAggregateStateFailures(
        string fileName,
        string content,
        IReadOnlyList<string> legacySetMethods,
        List<string> failures)
    {
        if (!content.Contains("AggregateGroup [", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing typed aggregate group shape");

        if (!content.Contains("TypedAggregateSet [", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing typed aggregate hot-loop update");

        if (content.Contains("CreateAggregateLibrary [", StringComparison.Ordinal))
            failures.Add($"{fileName}: typed aggregate still creates aggregate library state");

        if (!content.Contains(".Accumulate(", StringComparison.Ordinal) &&
            !content.Contains(".Set(ref ", StringComparison.Ordinal) &&
            !content.Contains(".Count = checked(", StringComparison.Ordinal) &&
            !content.Contains(".HasValue = true", StringComparison.Ordinal))
        {
            failures.Add($"{fileName}: missing typed aggregate update");
        }

        if (!content.Contains(".GetValue()", StringComparison.Ordinal) &&
            !content.Contains(".Get(in ", StringComparison.Ordinal) &&
            !content.Contains("finalGroup.__agg", StringComparison.Ordinal) &&
            !content.Contains("finalGroup.__owner", StringComparison.Ordinal) &&
            !content.Contains("FinalGroup.__agg", StringComparison.Ordinal) &&
            !content.Contains("FinalGroup.__owner", StringComparison.Ordinal))
        {
            failures.Add($"{fileName}: missing typed aggregate finalization");
        }

        foreach (var methodName in legacySetMethods)
        {
            if (content.Contains($"AggregateSet [{methodName}(", StringComparison.Ordinal) ||
                content.Contains($".{methodName}(", StringComparison.Ordinal))
            {
                failures.Add($"{fileName}: regular aggregate still uses {methodName} dictionary state");
            }
        }
    }

}
