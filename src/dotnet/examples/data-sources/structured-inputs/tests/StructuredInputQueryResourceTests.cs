using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

[TestClass]
public sealed class StructuredInputQueryResourceTests
{
    [TestMethod]
    public void QueryManifest_CompilesAndExecutesEveryAuthoredPositiveResource()
    {
        var root = FindRepositoryRoot();
        var queryDirectory = Path.Combine(root, "src", "dotnet", "examples", "data-sources", "structured-inputs", "queries");
        var manifestPath = Path.Combine(queryDirectory, "manifest.json");
        var cases = JsonSerializer.Deserialize<QueryCase[]>(
                        File.ReadAllText(manifestPath),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("The structured-input query manifest is empty.");

        var manifestFiles = cases.Select(static testCase => testCase.File).OrderBy(static file => file, StringComparer.Ordinal).ToArray();
        var authoredFiles = Directory.GetFiles(queryDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static file => file != null)
            .Cast<string>()
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(manifestFiles, authoredFiles, "Every positive SQL resource must have one manifest entry and no debug resource may be present.");

        foreach (var testCase in cases)
        {
            StructuredInputSourceCounters.Reset();
            var script = File.ReadAllText(Path.Combine(queryDirectory, testCase.File));
            var build = InstanceCreator.CompileWithDiagnostics(
                script,
                $"structured-input-resource-test-{testCase.Name}",
                new StructuredInputsSchemaProvider(),
                new NullLoggerResolver(),
                new CompilationOptions(usePrimitiveTypeValidation: false));
            Assert.IsTrue(
                build.Succeeded && build.CompiledQuery != null,
                $"Resource '{testCase.Name}' failed to compile: {build.CaughtException ?? new InvalidOperationException(string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())))}");

            using var compiled = build.CompiledQuery!;
            if (string.Equals(testCase.Host, "patterns", StringComparison.Ordinal))
                compiled.Parameters["patterns"] = CreateHostPatterns();

            using var table = compiled.Run();
            if (testCase.Columns is int expectedColumns)
                Assert.AreEqual(expectedColumns, table.Columns.Count(), testCase.Name);
            if (testCase.Rows is int expectedRows)
                Assert.AreEqual(expectedRows, table.Rows.Count, testCase.Name);
            if (testCase.First is { Length: > 0 })
            {
                Assert.IsTrue(table.Rows.Count > 0, $"Resource '{testCase.Name}' expected a first row.");
                Assert.IsTrue(testCase.First.Length <= table.Columns.Count(), testCase.Name);
                for (var index = 0; index < testCase.First.Length; index++)
                    Assert.IsTrue(Matches(testCase.First[index], table.Rows[0][index]), $"Resource '{testCase.Name}' first value {index} did not match.");
            }

            if (testCase.Counter is not null)
                Assert.AreEqual(1, ReadCounter(testCase.Counter), $"Resource '{testCase.Name}' source construction count.");
            if (testCase.MetadataOnly)
                Assert.AreEqual(0, ReadTotalCounter(), $"Resource '{testCase.Name}' must not construct a source.");
        }
    }

    private static List<Dictionary<string, object?>> CreateHostPatterns() =>
    [
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = "todo",
            ["Pattern"] = "TODO"
        },
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Pattern"] = "FIXME",
            ["Id"] = "fixme"
        }
    ];

    private static int ReadCounter(string name) => name.ToLowerInvariant() switch
    {
        "match" => StructuredInputSourceCounters.MatchConstructed,
        "configure" => StructuredInputSourceCounters.ConfigureConstructed,
        "numbers" => StructuredInputSourceCounters.NumbersConstructed,
        "matrix" => StructuredInputSourceCounters.MatrixConstructed,
        "weighted" => StructuredInputSourceCounters.WeightedConstructed,
        "overload" => StructuredInputSourceCounters.OverloadConstructed,
        "defaultconflict" => StructuredInputSourceCounters.DefaultConflictConstructed,
        "contextprobe" => StructuredInputSourceCounters.ContextProbeConstructed,
        "strict" => StructuredInputSourceCounters.StrictConstructed,
        "mutable" => StructuredInputSourceCounters.MutableConstructed,
        "throwing" => StructuredInputSourceCounters.ThrowingConstructed,
        _ => throw new InvalidOperationException($"Unknown source counter '{name}'.")
    };

    private static int ReadTotalCounter() =>
        StructuredInputSourceCounters.MatchConstructed +
        StructuredInputSourceCounters.ConfigureConstructed +
        StructuredInputSourceCounters.NumbersConstructed +
        StructuredInputSourceCounters.MatrixConstructed +
        StructuredInputSourceCounters.WeightedConstructed +
        StructuredInputSourceCounters.OverloadConstructed +
        StructuredInputSourceCounters.DefaultConflictConstructed +
        StructuredInputSourceCounters.ContextProbeConstructed +
        StructuredInputSourceCounters.StrictConstructed +
        StructuredInputSourceCounters.MutableConstructed +
        StructuredInputSourceCounters.ThrowingConstructed;

    private static bool Matches(JsonElement expected, object? actual) => expected.ValueKind switch
    {
        JsonValueKind.Null => actual is null,
        JsonValueKind.True => actual is true,
        JsonValueKind.False => actual is false,
        JsonValueKind.String => string.Equals(expected.GetString(), actual?.ToString(), StringComparison.Ordinal),
        JsonValueKind.Number when expected.TryGetInt64(out var integer) => actual switch
        {
            byte value => value == integer,
            short value => value == integer,
            int value => value == integer,
            long value => value == integer,
            _ => false
        },
        JsonValueKind.Number when expected.TryGetDecimal(out var decimalValue) => actual switch
        {
            float value => (decimal)value == decimalValue,
            double value => (decimal)value == decimalValue,
            decimal value => value == decimalValue,
            _ => false
        },
        _ => false
    };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "specs", "musoq-core-language-spec.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private sealed class QueryCase
    {
        public string Name { get; init; } = string.Empty;
        public string File { get; init; } = string.Empty;
        public int? Columns { get; init; }
        public int? Rows { get; init; }
        public JsonElement[]? First { get; init; }
        public string? Counter { get; init; }
        public string? Host { get; init; }
        public bool MetadataOnly { get; init; }
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger() => NullLogger.Instance;
        public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }
}
