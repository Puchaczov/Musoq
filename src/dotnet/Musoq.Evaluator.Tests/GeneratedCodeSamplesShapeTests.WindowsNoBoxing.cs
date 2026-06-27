using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void WindowSamples_WhenCheckedIn_ShouldNotEmitBoxedFallbackPaths()
    {
        var forbiddenPatterns = new[]
        {
            "WindowFunctionHelpers.CompositeKey",
            "WindowFunctionHelpers.ComputePluginWindowFunction",
            "WindowFunctionHelpers.ComputeFramedPluginWindowFunction",
            ".AccumulateValue(",
            ".GetCurrentValue(",
            "SetArguments(Array.Empty<object?>())",
            "new bool[]"
        };
        var failures = ReadSamples()
            .Where(static sample => sample.Content.Contains("Window", StringComparison.Ordinal))
            .SelectMany(sample => CreateBoxedWindowFallbackFailures(sample, forbiddenPatterns))
            .ToArray();

        Assert.IsEmpty(
            failures,
            $"Generated window samples should not emit boxed fallback paths: {string.Join("; ", failures)}");
    }

    private static IEnumerable<string> CreateBoxedWindowFallbackFailures(
        GeneratedCodeSampleFile sample,
        IReadOnlyList<string> forbiddenPatterns)
    {
        foreach (var forbiddenPattern in forbiddenPatterns)
        {
            if (sample.Content.Contains(forbiddenPattern, StringComparison.Ordinal))
                yield return $"{sample.FileName}: {forbiddenPattern}";
        }

        foreach (var line in sample.Content.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            if (!IsWindowInternalBufferLine(line))
                continue;

            if (line.Contains("object[]", StringComparison.Ordinal) ||
                line.Contains("new object[", StringComparison.Ordinal))
            {
                yield return $"{sample.FileName}: boxed window internal buffer: {line.Trim()}";
            }
        }
    }

    private static bool IsWindowInternalBufferLine(string line)
    {
        return line.Contains("PartitionKeys", StringComparison.Ordinal) ||
               line.Contains("OrderKeys", StringComparison.Ordinal) ||
               line.Contains("Values", StringComparison.Ordinal) ||
               line.Contains("Arguments", StringComparison.Ordinal);
    }
}
