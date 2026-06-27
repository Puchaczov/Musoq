using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static string[] GetAggregateOverHashJoinShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(AggregateOverHashJoinSampleFileName, content).ToList();

        if (!content.Contains("TryGetValue", StringComparison.Ordinal))
            failures.Add($"{AggregateOverHashJoinSampleFileName}: missing hash probe TryGetValue");

        AddTypedAggregateStateFailures(
            AggregateOverHashJoinSampleFileName,
            content,
            ["SetCount"],
            failures);

        if (content.Contains("_tableResults[0].Rows", StringComparison.Ordinal) ||
            content.Contains("_tableResults[0] = statement0", StringComparison.Ordinal) ||
            content.Contains("private sealed class Statement0Row0", StringComparison.Ordinal) ||
            content.Contains("private sealed class Statement1Row0", StringComparison.Ordinal))
        {
            failures.Add($"{AggregateOverHashJoinSampleFileName}: still materializes joined rows before grouping");
        }

        if (content.Contains("aRows.Rows", StringComparison.Ordinal) ||
            content.Contains("bRows.Rows", StringComparison.Ordinal))
        {
            failures.Add(
                $"{AggregateOverHashJoinSampleFileName}: source scans still iterate through RowSource.Rows");
        }

        return failures.ToArray();
    }

    private static string[] GetCteBackedAggregateOverHashJoinShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(CteBackedAggregateOverHashJoinSampleFileName, content).ToList();

        if (!content.Contains("Hash.TryGetValue", StringComparison.Ordinal))
            failures.Add($"{CteBackedAggregateOverHashJoinSampleFileName}: missing CTE-backed hash probe");

        if (!content.Contains("GetOrAddSingleKeyAggregateGroup [group = groups[l.City] by l.City; typed: ResultAggregateGroup]", StringComparison.Ordinal) ||
            !content.Contains("__musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg0.Count));", StringComparison.Ordinal))
        {
            failures.Add($"{CteBackedAggregateOverHashJoinSampleFileName}: aggregate-over-hash block is not lowered to typed final shapes");
        }

        AddTypedAggregateStateFailures(
            CteBackedAggregateOverHashJoinSampleFileName,
            content,
            ["SetCount"],
            failures);

        if (content.Contains("_tableResults[2]", StringComparison.Ordinal) ||
            content.Contains("private sealed class Statement0Row0", StringComparison.Ordinal) ||
            content.Contains("private sealed class Statement1Row0", StringComparison.Ordinal))
        {
            failures.Add($"{CteBackedAggregateOverHashJoinSampleFileName}: still materializes joined CTE rows before grouping");
        }

        if (content.Contains("aRows.Rows", StringComparison.Ordinal) ||
            content.Contains("bRows.Rows", StringComparison.Ordinal))
        {
            failures.Add(
                $"{CteBackedAggregateOverHashJoinSampleFileName}: CTE source scans still iterate through RowSource.Rows");
        }

        return failures.ToArray();
    }

    private static string[] GetRetiredHelperShapeFailures(string fileName, string content)
    {
        var failures = new List<string>();

        AddRetiredHelperShapeFailure(failures, fileName, SmartForEachPattern, content);
        AddRetiredHelperShapeFailure(failures, fileName, ConvertTableToSourcePattern, content);
        return failures.ToArray();
    }

    private static void AddRetiredHelperShapeFailure(
        List<string> failures,
        string fileName,
        string pattern,
        string content)
    {
        var count = CountOccurrences(content, pattern);
        if (count > 0)
            failures.Add($"{fileName}: contains {count} {pattern}");
    }

}
