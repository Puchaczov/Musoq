using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
/// Characterizes the structural-input preparation boundary and the typed
/// source path.  The benchmark deliberately keeps setup and query compilation
/// out of the execution methods except for the explicit parse/bind workload.
/// MemoryDiagnoser is the acceptance source for allocation data; ordinary
/// tests only verify correctness and generated shape.
/// </summary>
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
public class StructuredInputPreparationBenchmark
{
    private readonly BenchmarkLoggerResolver _loggerResolver = new();
    private PatternInput[] _typedPatterns = [];
    private WeightedInput[] _typedWeighted = [];
    private OptionsInput[] _typedNested = [];
    private int?[] _typedNullable = [];
    private IReadOnlyList<Dictionary<string, object?>> _hostPatterns = [];
    private IReadOnlyList<Dictionary<string, object?>> _hostWeighted = [];
    private IReadOnlyList<Dictionary<string, object?>> _hostNested = [];
    private int?[] _hostNullable = [];
    private int[] _hostCteValues = [];
    private CompiledQuery _inlineQuery = null!;
    private CompiledQuery _weightedQuery = null!;
    private CompiledQuery _nestedQuery = null!;
    private CompiledQuery _nullableQuery = null!;
    private CompiledQuery _emptyQuery = null!;
    private CompiledQuery _letQuery = null!;
    private CompiledQuery _hostQuery = null!;
    private CompiledQuery _cteQuery = null!;
    private CompiledQuery _scalarQuery = null!;
    private CompiledQuery _valuesQuery = null!;

    /// <summary>
    /// The structural node budget is 100,000. A host pattern cohort contributes
    /// one record and three scalar field nodes plus the declared Mode value per
    /// element. The integer record stride cannot land on every node boundary,
    /// so this is the largest cohort that remains within one stride of the
    /// one-node-below limit.
    /// </summary>
    public const int MaximumStructuralNodes = 100_000;
    public const int PatternRecordNodeCount = 5;
    public const int NearLimitSize = (MaximumStructuralNodes - 1) / PatternRecordNodeCount;

    public static IReadOnlyList<int> QualificationSizes { get; } =
        [1, 3, 32, 1_024, NearLimitSize];

    public static int PatternNodeCount(int size) =>
        checked(size * PatternRecordNodeCount);

    /// <summary>Input sizes used by the repeatable qualification matrix.</summary>
    [ParamsSource(nameof(QualificationSizes))]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _typedPatterns = CreateTypedPatterns(Size);
        _typedWeighted = CreateTypedWeighted(Size);
        _typedNested = CreateTypedNested(Size);
        _typedNullable = CreateTypedNullable(Size);
        _hostPatterns = CreateHostPatterns(Size);
        _hostWeighted = CreateHostWeighted(Size);
        // Nested records carry nine logical nodes per element.  Keep their
        // boundary fixture within the same global budget; the record cohort
        // above is the exact one-node-below case.
        _hostNested = CreateHostNested(
            Size == NearLimitSize
                ? (MaximumStructuralNodes - 1) / 9
                : Size);
        _hostNullable = [.. _typedNullable];
        _hostCteValues = Enumerable.Range(0, Size).ToArray();

        // A 33,333-record SQL literal is useful as a boundary-size shape but
        // makes the generated compilation artifact several gigabytes.  The
        // boundary cohort therefore uses the same generated structural
        // preparation through an owned host snapshot; representative cohorts
        // continue to use authored literals.  This keeps the benchmark
        // runnable while still charging the full near-limit capture path.
        var boundary = Size == NearLimitSize;
        _inlineQuery = Compile(
            boundary ? BuildHostQuery() : BuildInlineQuery(Size),
            $"structured-inline-{Size}");
        if (boundary)
            _inlineQuery.Parameters["patterns"] = _hostPatterns;

        _weightedQuery = Compile(
            boundary ? BuildWeightedHostQuery() : BuildWeightedQuery(Size),
            $"structured-weighted-{Size}");
        if (boundary)
            _weightedQuery.Parameters["items"] = _hostWeighted;

        _nestedQuery = Compile(
            boundary ? BuildNestedHostQuery() : BuildNestedQuery(Size),
            $"structured-nested-{Size}",
            new BenchmarkStructuredInputSchemaProvider());
        if (boundary)
            _nestedQuery.Parameters["options"] = _hostNested;

        _nullableQuery = Compile(
            boundary ? BuildNullableHostQuery() : BuildNullableQuery(Size),
            $"structured-nullable-{Size}",
            new BenchmarkStructuredInputSchemaProvider());
        if (boundary)
            _nullableQuery.Parameters["values"] = _hostNullable;

        _emptyQuery = Compile(BuildEmptyQuery(), $"structured-empty-{Size}");
        // Runtime parameters cannot participate in compile-time let
        // evaluation.  Keep the let workload small at the boundary while the
        // host and inline cohorts exercise the full input size.
        _letQuery = Compile(BuildLetQuery(boundary ? 1 : Size), $"structured-let-{Size}");
        _hostQuery = Compile(BuildHostQuery(), $"structured-host-{Size}");
        _cteQuery = Compile(
            boundary ? BuildCteHostQuery() : BuildCteQuery(Size),
            $"structured-cte-{Size}");
        if (boundary)
            _cteQuery.Parameters["values"] = _hostCteValues;
        _scalarQuery = Compile(BuildScalarQuery(), $"structured-scalar-{Size}");
        // VALUES remains a parser/source baseline.  Avoid producing another
        // multi-megabyte generated literal in the boundary setup; its
        // representative sizes are measured independently below.
        _valuesQuery = Compile(BuildValuesQuery(boundary ? 1 : Size), $"structured-values-{Size}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _inlineQuery.Dispose();
        _weightedQuery.Dispose();
        _nestedQuery.Dispose();
        _nullableQuery.Dispose();
        _emptyQuery.Dispose();
        _letQuery.Dispose();
        _hostQuery.Dispose();
        _cteQuery.Dispose();
        _scalarQuery.Dispose();
        _valuesQuery.Dispose();
    }

    /// <summary>
    /// Measures the explicit typed construction baseline.  It performs the
    /// same field validation/default work as the structural record path and
    /// owns the returned array for the operation.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Typed record construction")]
    public int TypedRecordConstruction()
    {
        var values = new PatternInput[Size];
        var checksum = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var source = _typedPatterns[index];
            if (string.IsNullOrEmpty(source.Id) || string.IsNullOrEmpty(source.Pattern))
                throw new InvalidOperationException("The typed baseline received an invalid pattern.");
            values[index] = new PatternInput(source.Id, source.Pattern, source.Mode);
            checksum += values[index].Id.Length;
        }

        return checksum;
    }

    [Benchmark(Description = "Structural record conversion")]
    public int StructuralRecordConstruction()
    {
        return ExecuteRows(_inlineQuery);
    }

    [Benchmark(Description = "Typed numeric struct construction")]
    public decimal TypedNumericStructConstruction()
    {
        var values = new WeightedInput[Size];
        decimal checksum = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var source = _typedWeighted[index];
            values[index] = new WeightedInput(source.Value, source.Weight, source.Enabled);
            checksum += values[index].Weight;
        }

        return checksum;
    }

    [Benchmark(Description = "Structural numeric struct conversion")]
    public decimal StructuralNumericStructConstruction()
    {
        return ExecuteDecimalRows(_weightedQuery);
    }

    [Benchmark(Description = "Typed nested construction")]
    public int TypedNestedConstruction()
    {
        var values = new OptionsInput[Size];
        var checksum = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var source = _typedNested[index];
            values[index] = new OptionsInput(
                source.Enabled,
                [.. source.Codes],
                new WindowInput(source.Window.Before, source.Window.After));
            checksum += values[index].Codes.Length + values[index].Window.Before + values[index].Window.After;
        }

        return checksum;
    }

    [Benchmark(Description = "Structural nested conversion")]
    public int StructuralNestedConstruction()
    {
        return ExecuteIntRows(_nestedQuery);
    }

    [Benchmark(Description = "Typed nullable construction")]
    public int TypedNullableConstruction()
    {
        var values = new int?[Size];
        var checksum = 0;
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = _typedNullable[index];
            checksum += values[index] ?? 0;
        }

        return checksum;
    }

    [Benchmark(Description = "Structural nullable conversion")]
    public int StructuralNullableConstruction()
    {
        return ExecuteNullableRows(_nullableQuery);
    }

    [Benchmark(Description = "Structural empty conversion")]
    public int StructuralEmptyConstruction()
    {
        return ExecuteRows(_emptyQuery);
    }

    [Benchmark(Description = "Parse, bind, and metadata discovery")]
    public int ParseBindAndMetadata()
    {
        var inspection = InstanceCreator.CompileForInspection(
            Size == NearLimitSize ? BuildHostQuery() : BuildInlineQuery(Size),
            $"structured-inspection-{Size}-{Guid.NewGuid():N}",
            new StructuredInputsSchemaProvider(),
            _loggerResolver,
            CompilationOptionsForBenchmark());
        return inspection.GeneratedCSharpCode.Length;
    }

    [Benchmark(Description = "Inline source invocation")]
    public int InlineSourceInvocation() => ExecuteRows(_inlineQuery);

    [Benchmark(Description = "Retained let source invocation")]
    public int RetainedLetSourceInvocation() => ExecuteRows(_letQuery);

    [Benchmark(Description = "Host normalization and source invocation")]
    public int HostNormalizationAndSourceInvocation()
    {
        _hostQuery.Parameters["patterns"] = _hostPatterns;
        return ExecuteRows(_hostQuery);
    }

    [Benchmark(Description = "CTE production and adaptation")]
    public int CteProductionAndAdaptation() => ExecuteRows(_cteQuery);

    [Benchmark(Description = "Result enumeration after typed preparation")]
    public int ResultEnumerationAfterPreparation()
    {
        var rows = StructuredInputOperations.Match("TODO", _typedPatterns);
        var checksum = 0;
        for (var index = 0; index < rows.Length; index++)
            checksum += rows[index].PatternId.Length;
        return checksum;
    }

    [Benchmark(Description = "Scalar source invocation cohort")]
    public int ScalarSourceInvocation() => ExecuteRows(_scalarQuery);

    [Benchmark(Description = "VALUES source invocation cohort")]
    public int ValuesSourceInvocation() => ExecuteRows(_valuesQuery);

    /// <summary>Creates the values used by correctness and shape tests.</summary>
    public static string BuildInlineQuery(int size)
    {
        return $"select m.PatternId from #inputs.match('TODO', patterns: array {{ {BuildPatternLiterals(size)} }}) m";
    }

    public static string BuildLetQuery(int size)
    {
        return $"let patterns = array {{ {BuildPatternLiterals(size)} }}; select m.PatternId from #inputs.match('TODO', patterns: $patterns) m";
    }

    public static string BuildWeightedQuery(int size)
    {
        return $"select w.Weight from #inputs.weighted(items: array {{ {BuildWeightedLiterals(size)} }}) w";
    }

    public static string BuildWeightedHostQuery() =>
        "param(items: (Value: int, Weight: decimal, Enabled: bool)[]) " +
        "select w.Weight from #inputs.weighted(items: $items) w";

    public static string BuildNestedQuery(int size)
    {
        return $"select o.CodeCount + o.Before + o.After from #benchmarkinputs.options(options: array {{ {BuildNestedLiterals(size)} }}) o";
    }

    public static string BuildNestedHostQuery() =>
        "param(options: (Enabled: bool, Codes: int[], Window: (Before: int, After: int))[]) " +
        "select o.CodeCount + o.Before + o.After from #benchmarkinputs.options(options: $options) o";

    public static string BuildNullableQuery(int size)
    {
        return $"param(values: int?[] = array {{ {BuildNullableLiterals(size)} }}) select n.Value from #benchmarkinputs.nullable(values: $values) n";
    }

    public static string BuildNullableHostQuery() =>
        "param(values: int?[]) select n.Value from #benchmarkinputs.nullable(values: $values) n";

    public static string BuildEmptyQuery() =>
        "select n.Value from #inputs.numbers(values: array {}) n";

    public static string BuildHostQuery()
    {
        return "param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[]) select m.PatternId from #inputs.match('TODO', patterns: $patterns) m";
    }

    public static string BuildCteQuery(int size)
    {
        return $"with patterns as (select p.Id, p.Pattern from values {{ {BuildPatternLiterals(size)} }} p) select m.PatternId from #inputs.match('TODO', patterns: patterns) m";
    }

    public static string BuildCteHostQuery() =>
        "param(values: int[]) " +
        "with numbers as (select n.Value from #inputs.numbers(values: $values) n) " +
        "select n.Value from #inputs.numbers(values: numbers) n";

    public static string BuildScalarQuery() =>
        "select c.Alias from #inputs.contextprobe() c";

    public static string BuildValuesQuery(int size)
    {
        var builder = new StringBuilder(Math.Max(64, size * 16));
        builder.Append("select p.Value from values { ");
        for (var index = 0; index < size; index++)
        {
            if (index != 0)
                builder.Append(", ");
            builder.Append("(Value: ").Append(index.ToString(CultureInfo.InvariantCulture)).Append(')');
        }

        return builder.Append(" } p").ToString();
    }

    internal static PatternInput[] CreateTypedPatterns(int size)
    {
        var values = new PatternInput[size];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = new PatternInput(
                $"id-{index.ToString(CultureInfo.InvariantCulture)}",
                "TODO");
        }

        return values;
    }

    private static WeightedInput[] CreateTypedWeighted(int size)
    {
        var values = new WeightedInput[size];
        for (var index = 0; index < values.Length; index++)
            values[index] = new WeightedInput(index, index + 0.5m, index % 2 == 0);
        return values;
    }

    private static OptionsInput[] CreateTypedNested(int size)
    {
        var values = new OptionsInput[size];
        for (var index = 0; index < values.Length; index++)
            values[index] = new OptionsInput(index % 2 == 0, [index, index + 1], new WindowInput(index, index + 1));
        return values;
    }

    private static int?[] CreateTypedNullable(int size)
    {
        var values = new int?[size];
        for (var index = 0; index < values.Length; index++)
            values[index] = index % 3 == 0 ? null : index;
        return values;
    }

    private static IReadOnlyList<Dictionary<string, object?>> CreateHostPatterns(int size)
    {
        var values = new Dictionary<string, object?>[size];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = $"id-{index.ToString(CultureInfo.InvariantCulture)}",
                ["Pattern"] = "TODO",
                ["Mode"] = "literal"
            };
        }

        return values;
    }

    private static IReadOnlyList<Dictionary<string, object?>> CreateHostWeighted(int size)
    {
        var values = new Dictionary<string, object?>[size];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Value"] = index,
                ["Weight"] = index + 0.5m,
                ["Enabled"] = index % 2 == 0
            };
        }

        return values;
    }

    private static IReadOnlyList<Dictionary<string, object?>> CreateHostNested(int size)
    {
        var values = new Dictionary<string, object?>[size];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Enabled"] = index % 2 == 0,
                ["Codes"] = new[] { index, index + 1 },
                ["Window"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Before"] = index,
                    ["After"] = index + 1
                }
            };
        }

        return values;
    }

    private static string BuildPatternLiterals(int size)
    {
        var builder = new StringBuilder(Math.Max(64, size * 36));
        for (var index = 0; index < size; index++)
        {
            if (index != 0)
                builder.Append(", ");
            builder.Append("(Id: '")
                .Append("id-")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append("', Pattern: 'TODO')");
        }

        return builder.ToString();
    }

    private static string BuildWeightedLiterals(int size)
    {
        var builder = new StringBuilder(Math.Max(64, size * 44));
        for (var index = 0; index < size; index++)
        {
            if (index != 0)
                builder.Append(", ");
            builder.Append("(Value: ")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append(", Weight: ")
                .Append((index + 0.5m).ToString(CultureInfo.InvariantCulture))
                .Append(", Enabled: ")
                .Append(index % 2 == 0 ? "true" : "false")
                .Append(')');
        }

        return builder.ToString();
    }

    private static string BuildNestedLiterals(int size)
    {
        var builder = new StringBuilder(Math.Max(64, size * 80));
        for (var index = 0; index < size; index++)
        {
            if (index != 0)
                builder.Append(", ");
            builder.Append("(Enabled: ")
                .Append(index % 2 == 0 ? "true" : "false")
                .Append(", Codes: array { ")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append(", ")
                .Append((index + 1).ToString(CultureInfo.InvariantCulture))
                .Append(" }, Window: (Before: ")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append(", After: ")
                .Append((index + 1).ToString(CultureInfo.InvariantCulture))
                .Append("))");
        }

        return builder.ToString();
    }

    private static string BuildNullableLiterals(int size)
    {
        var builder = new StringBuilder(Math.Max(32, size * 8));
        for (var index = 0; index < size; index++)
        {
            if (index != 0)
                builder.Append(", ");
            if (index % 3 == 0)
                builder.Append("null");
            else
                builder.Append(index.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private CompiledQuery Compile(
        string query,
        string name,
        ISchemaProvider? schemaProvider = null)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            name,
            schemaProvider ?? new StructuredInputsSchemaProvider(),
            _loggerResolver,
            CompilationOptionsForBenchmark());
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        }

        return result.CompiledQuery!;
    }

    private static CompilationOptions CompilationOptionsForBenchmark() =>
        new CompilationOptions(ParallelizationMode.None).WithTableResultMaterialization();

    private static int ExecuteRows(CompiledQuery query)
    {
        using var table = query.Run();
        var checksum = 0;
        foreach (var row in table.Rows)
            checksum += row[0]?.ToString()?.Length ?? 0;
        return checksum;
    }

    private static decimal ExecuteDecimalRows(CompiledQuery query)
    {
        using var table = query.Run();
        decimal checksum = 0;
        foreach (var row in table.Rows)
            checksum += row[0] is decimal value ? value : Convert.ToDecimal(row[0], CultureInfo.InvariantCulture);
        return checksum;
    }

    private static int ExecuteIntRows(CompiledQuery query)
    {
        using var table = query.Run();
        var checksum = 0;
        foreach (var row in table.Rows)
            checksum += row[0] is int value ? value : Convert.ToInt32(row[0], CultureInfo.InvariantCulture);
        return checksum;
    }

    private static int ExecuteNullableRows(CompiledQuery query)
    {
        using var table = query.Run();
        var checksum = 0;
        foreach (var row in table.Rows)
            checksum += row[0] is int value ? value : 0;
        return checksum;
    }
}
