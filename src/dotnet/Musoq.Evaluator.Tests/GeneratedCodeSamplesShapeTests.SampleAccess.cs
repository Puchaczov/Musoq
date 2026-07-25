using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static readonly Lazy<GeneratedCodeSampleFile[]> GeneratedSamples = new(CreateGeneratedSamples);

    private static GeneratedCodeSampleFile[] ReadSamples()
    {
        return GeneratedSamples.Value;
    }

    private static GeneratedCodeSampleFile[] CreateGeneratedSamples()
    {
        var loggerResolver = new TestsLoggerResolver();

        return GeneratedCodeSamplesCatalog.Samples
            .Select(sample => new GeneratedCodeSampleFile(
                sample.FileName,
                sample.Category,
                GeneratedCodeSampleArtifacts.Generate(sample, loggerResolver)))
            .ToArray();
    }

    private static string ReadGeneratedSampleSection(
        string content,
        string sectionName,
        string? nextSectionName)
    {
        var marker = $"// === {sectionName} ===";
        var start = content.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidOperationException($"Generated sample section '{sectionName}' is missing.");

        start += marker.Length;
        if (nextSectionName is null)
            return content[start..];

        var end = content.IndexOf($"// === {nextSectionName} ===", start, StringComparison.Ordinal);
        if (end < 0)
            throw new InvalidOperationException($"Generated sample section '{nextSectionName}' is missing.");

        return content[start..end];
    }

    private static IEnumerable<GeneratedRowCastOccurrence> CreateGeneratedRowCastOccurrences(
        GeneratedCodeSampleFile sample)
    {
        return GeneratedRowCastPattern
            .Matches(sample.Content)
            .Cast<Match>()
            .Select(match => CreateGeneratedRowCastOccurrence(sample, match));
    }

    private static GeneratedRowCastOccurrence CreateGeneratedRowCastOccurrence(
        GeneratedCodeSampleFile sample,
        Match match)
    {
        var rowType = match.Groups["rowType"].Value;
        var sourceName = match.Groups["sourceName"].Value;
        var line = GetLine(sample.Content, match.Index);

        return new GeneratedRowCastOccurrence(
            sample.FileName,
            line,
            CategorizeGeneratedRowCast(sample.FileName, line, rowType, sourceName));
    }

    private static GeneratedRowCastCategory CategorizeGeneratedRowCast(
        string fileName,
        string line,
        string rowType,
        string sourceName)
    {
        if (fileName == DynamicCteBackedAsOfJoinSampleFileName)
            return GeneratedRowCastCategory.DynamicFallback;

        if (IsResultSortGeneratedRowCast(line))
            return GeneratedRowCastCategory.ResultSortRows;

        if (fileName == CteBackedAsOfJoinSampleFileName)
            return GeneratedRowCastCategory.AsOfCandidates;

        if (NestedInterpretationExpansionSampleFileNames.Contains(fileName, StringComparer.Ordinal))
            return GeneratedRowCastCategory.InterpretationApplyRows;

        if (IsWindowSourceGeneratedRowCast(rowType, sourceName))
            return GeneratedRowCastCategory.WindowSourceRows;

        if (rowType.StartsWith("Cte", StringComparison.Ordinal))
            return GeneratedRowCastCategory.CteRows;

        return GeneratedRowCastCategory.Uncategorized;
    }

    private static bool IsResultSortGeneratedRowCast(string line)
    {
        return line.Contains("resultSortedRows = result.Rows.OrderBy", StringComparison.Ordinal);
    }

    private static bool IsWindowSourceGeneratedRowCast(string rowType, string sourceName)
    {
        return rowType == "WindowSourceRow0" ||
               rowType == "apply_0_i_n_mRow0" ||
               sourceName is "ab" or "ba" or "inmScore";
    }

    private static string GetLine(string content, int index)
    {
        var lineStart = content.LastIndexOf('\n', index);
        var lineEnd = content.IndexOf('\n', index);
        var start = lineStart < 0 ? 0 : lineStart + 1;
        var length = (lineEnd < 0 ? content.Length : lineEnd) - start;

        return content.Substring(start, length).Trim();
    }

    private static QueryInspectionResult CompileSampleForInspection(string fileName)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(fileName);

        return InstanceCreator.CompileForInspection(
            sample.Query,
            $"GeneratedSample_{Path.GetFileNameWithoutExtension(sample.FileName)}",
            sample.CreateSchemaProvider(),
            new TestsLoggerResolver(),
            sample.CompilationOptions);
    }

    private static QueryInspectionResult CompileSampleForInspection(
        string fileName,
        CompilationOptions compilationOptions)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(fileName);

        return InstanceCreator.CompileForInspection(
            sample.Query,
            $"GeneratedSample_{Path.GetFileNameWithoutExtension(sample.FileName)}",
            sample.CreateSchemaProvider(),
            new TestsLoggerResolver(),
            compilationOptions);
    }

    private static CompiledQuery CompileSampleForExecution(string fileName)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(fileName);

        return InstanceCreator.CompileForExecution(
            sample.Query,
            $"GeneratedSample_{Path.GetFileNameWithoutExtension(sample.FileName)}_Execution",
            sample.CreateSchemaProvider(),
            new TestsLoggerResolver(),
            sample.CompilationOptions);
    }

    private static QueryInspectionResult CompileBasicQueryForInspection(string query)
    {
        return InstanceCreator.CompileForInspection(
            query,
            "GeneratedSample_BasicInspection",
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    { "#A", Array.Empty<BasicEntity>() }
                }),
            new TestsLoggerResolver());
    }

    private static void AssertUsesExecutionBackendWithoutRetiredHelperPatterns(QueryInspectionResult result)
    {
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    private static void AssertUsesTypedAggregateState(
        QueryInspectionResult result,
        params string[] legacySetMethods)
    {
        Assert.Contains("AggregateGroup [", result.ExecutionPlanText);
        Assert.Contains("TypedAggregateSet [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("CreateAggregateLibrary [", StringComparison.Ordinal));
        Assert.IsTrue(
            result.GeneratedCSharpCode.Contains(".Accumulate(", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains(".Set(ref ", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains(".Count = checked(", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains(".HasValue = true", StringComparison.Ordinal),
            result.GeneratedCSharpCode);
        Assert.IsTrue(
            result.GeneratedCSharpCode.Contains(".GetValue()", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains(".Get(in ", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains("finalGroup.__agg", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains("finalGroup.__owner", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains("FinalGroup.__agg", StringComparison.Ordinal) ||
            result.GeneratedCSharpCode.Contains("FinalGroup.__owner", StringComparison.Ordinal),
            result.GeneratedCSharpCode);

        foreach (var methodName in legacySetMethods)
        {
            Assert.IsFalse(
                result.ExecutionPlanText.Contains($"AggregateSet [{methodName}(", StringComparison.Ordinal),
                result.ExecutionPlanText);
            Assert.IsFalse(
                result.GeneratedCSharpCode.Contains($".{methodName}(", StringComparison.Ordinal),
                result.GeneratedCSharpCode);
        }
    }

}
