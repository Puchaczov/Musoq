using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static ShapeBudget CountShapes(IEnumerable<GeneratedCodeSampleFile> samples)
    {
        var sampleArray = samples.ToArray();

        return new ShapeBudget
        {
            GetColumnValue = CountOccurrences(sampleArray, GetColumnValuePattern),
            ConvertTableToSource = CountOccurrences(sampleArray, ConvertTableToSourcePattern),
            ConvertTableToSourceWithDiscardedContexts = CountOccurrences(
                sampleArray,
                ConvertTableToSourceWithDiscardedContextsPattern),
            TableRowSource = CountOccurrences(sampleArray, TableRowSourcePattern),
            ObjectResolver = CountOccurrences(sampleArray, ObjectResolverPattern),
            SmartForEach = CountOccurrences(sampleArray, SmartForEachPattern),
            ContextsAccess = CountOccurrences(sampleArray, ContextsAccessPattern),
            DynamicDictionaryRead = CountOccurrences(sampleArray, MutableDynamicDictionaryPattern)
        };
    }

    private static int CountOccurrences(IEnumerable<GeneratedCodeSampleFile> samples, Regex pattern)
    {
        return samples.Sum(sample => pattern.Count(sample.Content));
    }

    private static int CountOccurrences(IEnumerable<GeneratedCodeSampleFile> samples, params string[] patterns)
    {
        return samples.Sum(sample => patterns.Sum(pattern => CountOccurrences(sample.Content, pattern)));
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var searchStart = 0;

        while (searchStart < text.Length)
        {
            var index = text.IndexOf(pattern, searchStart, StringComparison.Ordinal);

            if (index < 0)
                return count;

            count++;
            searchStart = index + pattern.Length;
        }

        return count;
    }

    private static string GetComputeMethod(string content)
    {
        var start = content.IndexOf("        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0", StringComparison.Ordinal);
        if (start < 0)
            start = content.IndexOf("        private Table ComputeTable_compiled_0", StringComparison.Ordinal);
        if (start < 0)
            start = content.IndexOf("        private IEnumerable<ResultRow0> ComputeRows_compiled_0", StringComparison.Ordinal);

        if (start < 0)
            throw new AssertFailedException("Generated sample does not contain the expected compute table or rows method start.");

        var openBrace = content.IndexOf('{', start);

        if (openBrace < 0)
            throw new AssertFailedException("Generated sample does not contain the expected compute method body.");

        var closeBrace = FindMatchingBrace(content, openBrace);

        if (closeBrace < 0)
            throw new AssertFailedException("Generated sample contains an unbalanced compute method.");

        return content.Substring(start, closeBrace - start + 1);
    }

    private static void AssertScriptParameterBinderCount(string fileName, string content, int expectedCount)
    {
        Assert.AreEqual(
            expectedCount,
            CountOccurrences(content, "ScriptParameterBinder.Get"),
            $"{fileName}: script parameters should be bound once in the top-level generated query method.");
    }

    private static void AssertTopLevelBindingBefore(
        string fileName,
        string content,
        string bindingPattern,
        string laterPattern)
    {
        var computeMethod = GetComputeMethod(content);
        var bindingIndex = computeMethod.IndexOf(bindingPattern, StringComparison.Ordinal);
        var laterIndex = computeMethod.IndexOf(laterPattern, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(
            0,
            bindingIndex,
            $"{fileName}: missing top-level parameter binding '{bindingPattern}'.");
        Assert.IsGreaterThanOrEqualTo(
            0,
            laterIndex,
            $"{fileName}: missing top-level generated helper/source use '{laterPattern}'.");
        Assert.IsLessThan(
            laterIndex,
            bindingIndex,
            $"{fileName}: parameter binding should happen before helper invocation or source use.");
    }

    private static void AssertStaticHelpersDoNotReadRuntimeParameters(string fileName, string content)
    {
        var failures = GetPrivateStaticMethods(content)
            .Where(static method => method.Body.Contains("ScriptParameterBinder.", StringComparison.Ordinal) ||
                                    method.Body.Contains("Parameters", StringComparison.Ordinal))
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            failures,
            $"{fileName}: generated static helper(s) read runtime parameters: {string.Join(", ", failures)}");
    }

    private static void AssertRowLoopsDoNotReadRuntimeParameters(string fileName, string content)
    {
        var loopFailures = new List<string>();
        var methods = GetPrivateStaticMethods(content)
            .Prepend(new GeneratedMethod("Compute_compiled_0", GetComputeMethod(content)));

        foreach (var method in methods)
        {
            foreach (var loopBody in GetLoopBlocks(method.Body))
            {
                if (loopBody.Contains("ScriptParameterBinder.", StringComparison.Ordinal) ||
                    loopBody.Contains("Parameters", StringComparison.Ordinal))
                {
                    loopFailures.Add(method.Name);
                    break;
                }
            }
        }

        Assert.IsEmpty(
            loopFailures,
            $"{fileName}: generated row loop(s) read runtime parameters in {string.Join(", ", loopFailures)}");
    }

    private static List<GeneratedMethod> GetPrivateStaticMethods(string content)
    {
        var methods = new List<GeneratedMethod>();
        var searchStart = 0;
        const string marker = "        private static ";

        while (searchStart < content.Length)
        {
            var start = content.IndexOf(marker, searchStart, StringComparison.Ordinal);
            if (start < 0)
                break;

            var lineEnd = content.IndexOf('\n', start);
            var openBrace = content.IndexOf('{', start);
            var openParen = content.IndexOf('(', start);

            if (openBrace < 0 || openParen < 0 || openParen > openBrace ||
                (lineEnd >= 0 && openParen > lineEnd) ||
                content[start..Math.Min(openBrace, content.Length)].Contains(" readonly ", StringComparison.Ordinal))
            {
                searchStart = Math.Max(start + marker.Length, lineEnd < 0 ? start + marker.Length : lineEnd + 1);
                continue;
            }

            var closeBrace = FindMatchingBrace(content, openBrace);
            if (closeBrace < 0)
                throw new AssertFailedException("Generated sample contains an unbalanced private static helper method.");

            var declaration = content[start..openBrace];
            methods.Add(new GeneratedMethod(
                ExtractMethodName(declaration),
                content.Substring(openBrace, closeBrace - openBrace + 1)));
            searchStart = closeBrace + 1;
        }

        return methods;
    }

    private static IReadOnlyList<string> GetLoopBlocks(string content)
    {
        return [.. GetKeywordBlocks(content, "foreach "), .. GetKeywordBlocks(content, "for (")];
    }

    private static IEnumerable<string> GetKeywordBlocks(string content, string keyword)
    {
        var searchStart = 0;

        while (searchStart < content.Length)
        {
            var start = content.IndexOf(keyword, searchStart, StringComparison.Ordinal);
            if (start < 0)
                yield break;

            var openBrace = content.IndexOf('{', start);
            if (openBrace < 0)
                yield break;

            var closeBrace = FindMatchingBrace(content, openBrace);
            if (closeBrace < 0)
                throw new AssertFailedException($"Generated sample contains an unbalanced '{keyword.Trim()}' block.");

            yield return content.Substring(openBrace, closeBrace - openBrace + 1);
            searchStart = closeBrace + 1;
        }
    }

    private static int FindMatchingBrace(string content, int openBraceIndex)
    {
        var depth = 0;

        for (var index = openBraceIndex; index < content.Length; index++)
        {
            switch (content[index])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                        return index;
                    break;
            }
        }

        return -1;
    }

    private static string ExtractMethodName(string declaration)
    {
        var parameterListStart = declaration.IndexOf('(');
        if (parameterListStart < 0)
            throw new AssertFailedException("Generated helper declaration has no parameter list.");

        var beforeParameters = declaration[..parameterListStart].TrimEnd();
        var nameStart = beforeParameters.LastIndexOfAny([' ', '\t', '\r', '\n']);
        return beforeParameters[(nameStart + 1)..];
    }

    private static bool ContainsInlineArrayIndexOf(string content)
    {
        var compact = Regex.Replace(content, @"\s+", string.Empty);

        return compact.Contains("Array.IndexOf(new", StringComparison.Ordinal);
    }

    private static string[] GetSourceScanShapeFailures(
        Dictionary<string, string> samplesByFileName,
        SourceScanShapeExpectation expectation)
    {
        if (!samplesByFileName.TryGetValue(expectation.FileName, out var content))
            return [$"{expectation.FileName}: missing checked-in sample"];

        var failures = new List<string>();
        var resolverLoop = $"{expectation.Alias}Rows.Rows";

        if (!HasSplitTypedRowSourceDeclaration(content, expectation.Alias))
            failures.Add($"{expectation.FileName}: {expectation.Alias} source scan misses generic RowSource<T> call");

        if (!HasDirectTypedRowsLoop(content, expectation))
            failures.Add($"{expectation.FileName}: {expectation.Alias} source scan misses direct typed consumption");

        if (content.Contains(resolverLoop, StringComparison.Ordinal))
            failures.Add($"{expectation.FileName}: {expectation.Alias} source scan still loops over RowSource.Rows");

        return failures.ToArray();
    }

    private static bool HasSplitTypedRowSourceDeclaration(string content, string alias)
    {
        var escapedAlias = Regex.Escape(alias);
        var rowSourcePattern =
            $@"var [A-Za-z0-9_]*{escapedAlias}RowsSource = __[A-Za-z0-9_]*{escapedAlias}Schema\.GetRowSource<";
        var rowsPattern =
            $@"var [A-Za-z0-9_]*{escapedAlias}Rows = [A-Za-z0-9_]*{escapedAlias}RowsSource\.Chunks;";

        return Regex.IsMatch(content, rowSourcePattern) && Regex.IsMatch(content, rowsPattern);
    }

    private static bool HasDirectTypedRowsLoop(string content, SourceScanShapeExpectation expectation)
    {
        if (!string.IsNullOrEmpty(expectation.DirectUsePattern) &&
            content.Contains(expectation.DirectUsePattern, StringComparison.Ordinal))
        {
            return true;
        }

        var directLoopPattern = $@"foreach \(var {Regex.Escape(expectation.Alias)}Chunk in [A-Za-z0-9_]*{Regex.Escape(expectation.Alias)}Rows\)";
        if (Regex.IsMatch(content, directLoopPattern))
            return true;

        var parallelAggregateRowsPattern =
            $@"EvaluationHelper\.GetParallelAggregationRowsOrEmpty<[^>]+>\([A-Za-z0-9_]*{Regex.Escape(expectation.Alias)}Rows,";
        if (Regex.IsMatch(content, parallelAggregateRowsPattern))
            return true;

        var valueTupleAggregateHelperPattern =
            $@"Populate[A-Za-z0-9]*Groups\([A-Za-z0-9_]*{Regex.Escape(expectation.Alias)}Rows,";
        if (Regex.IsMatch(content, valueTupleAggregateHelperPattern))
            return true;

        var chunkMaterializePattern =
            $@"EvaluationHelper\.Materialize(?:Generated)?ChunkedRows(?:List)?\([A-Za-z0-9_]*{Regex.Escape(expectation.Alias)}Rows\)";
        if (Regex.IsMatch(content, chunkMaterializePattern))
            return true;

        var directProjectionSourcePattern =
            $@"var __musoqTableSourceRows = [A-Za-z0-9_]*{Regex.Escape(expectation.Alias)}Rows;";
        return Regex.IsMatch(content, directProjectionSourcePattern);
    }

}
