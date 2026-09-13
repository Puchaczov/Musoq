using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Examples.DataSources.StructuredInputs;

if (!args.Contains("--all", StringComparer.Ordinal))
{
    Console.WriteLine("Use --all to run every structured-input example.");
    return;
}

var repositoryRoot = FindRepositoryRoot();
var queryDirectory = Path.Combine(
    repositoryRoot,
    "src",
    "dotnet",
    "examples",
    "data-sources",
    "structured-inputs",
    "queries");
var manifestPath = Path.Combine(queryDirectory, "manifest.json");
var cases = JsonSerializer.Deserialize<QueryCase[]>(
                File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The structured-input query manifest is empty.");

var nativePatterns = new PatternInput[]
{
    new("todo", "TODO"),
    new("fixme", "FIXME"),
    new("issue", "ISSUE-[0-9]+", "regex")
};
var nativeMatches = StructuredInputOperations.Match("TODO FIXME ISSUE-42", nativePatterns);
var nativeNumbers = StructuredInputOperations.Numbers([1, 2, 2, 3]);
var nativeMatrix = StructuredInputOperations.Matrix([[1, 2], [3], []]);
var nativeWeighted = StructuredInputOperations.Weighted([
    new WeightedInput(10, 1.5m),
    new WeightedInput(20, 2.5m, false)
]);

Console.WriteLine($"native.match={nativeMatches.Length} first={nativeMatches[0].PatternId}:{nativeMatches[0].Offset}");
Console.WriteLine($"native.numbers={string.Join(',', nativeNumbers.Select(static row => row.Value))}");
Console.WriteLine($"native.matrix={string.Join(',', nativeMatrix.Select(static row => row.Value))}");
Console.WriteLine($"native.weighted={string.Join(',', nativeWeighted.Select(static row => $"{row.Value}:{row.Weight.ToString(CultureInfo.InvariantCulture)}:{row.Enabled}"))}");

foreach (var testCase in cases)
{
    var queryPath = Path.Combine(queryDirectory, testCase.File);
    if (!File.Exists(queryPath))
        throw new InvalidOperationException($"Manifest entry '{testCase.Name}' points to missing query '{queryPath}'.");

    StructuredInputSourceCounters.Reset();
    var script = File.ReadAllText(queryPath);
    var build = InstanceCreator.CompileWithDiagnostics(
        script,
        $"structured-input-example-{testCase.Name}",
        new StructuredInputsSchemaProvider(),
        new NullLoggerResolver(),
        new CompilationOptions(usePrimitiveTypeValidation: false));
    if (!build.Succeeded || build.CompiledQuery is null)
    {
        var details = build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString()));
        throw new InvalidOperationException($"Example '{testCase.Name}' failed to compile.{Environment.NewLine}{details}");
    }

    using var compiled = build.CompiledQuery;
    if (string.Equals(testCase.Host, "patterns", StringComparison.Ordinal))
        compiled.Parameters["patterns"] = CreateHostPatterns();

    using var table = compiled.Run();
    var columns = table.Columns.Count();
    var rows = table.Rows.Count;
    if (testCase.Columns is int expectedColumns && columns != expectedColumns)
        throw new InvalidOperationException($"Example '{testCase.Name}' returned {columns} columns; expected {expectedColumns}.");
    if (testCase.Rows is int expectedRows && rows != expectedRows)
        throw new InvalidOperationException($"Example '{testCase.Name}' returned {rows} rows; expected {expectedRows}.");
    if (testCase.First is { Length: > 0 })
    {
        if (rows == 0)
            throw new InvalidOperationException($"Example '{testCase.Name}' has expected first-row values but returned no rows.");
        var firstRow = table.Rows[0];
        if (testCase.First.Length > columns)
            throw new InvalidOperationException($"Example '{testCase.Name}' specifies more first-row values than columns.");
        for (var index = 0; index < testCase.First.Length; index++)
        {
            if (!Matches(testCase.First[index], firstRow[index]))
                throw new InvalidOperationException($"Example '{testCase.Name}' first-row column {index} was '{firstRow[index]}', which does not match the manifest.");
        }
    }

    if (testCase.Counter is not null && ReadCounter(testCase.Counter) != 1)
        throw new InvalidOperationException($"Example '{testCase.Name}' did not construct its source exactly once.");
    if (testCase.MetadataOnly && ReadTotalCounter() != 0)
        throw new InvalidOperationException($"Description example '{testCase.Name}' constructed a source.");

    Console.WriteLine($"sql={testCase.Name} columns={columns} rows={rows}");
}

static List<Dictionary<string, object?>> CreateHostPatterns()
{
    return new List<Dictionary<string, object?>>
    {
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
    };
}
static int ReadCounter(string name)
{
    return name.ToLowerInvariant() switch
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
}

static int ReadTotalCounter()
{
    return StructuredInputSourceCounters.MatchConstructed +
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
}

static bool Matches(JsonElement expected, object? actual)
{
    return expected.ValueKind switch
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
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "specs", "musoq-core-language-spec.md")))
        directory = directory.Parent;
    return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
}

sealed class QueryCase
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

sealed class NullLoggerResolver : ILoggerResolver
{
    public ILogger ResolveLogger() => NullLogger.Instance;
    public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
}
