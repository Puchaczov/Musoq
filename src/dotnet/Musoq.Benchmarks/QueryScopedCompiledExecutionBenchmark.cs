using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Examples.DataSources.Csv;

namespace Musoq.Benchmarks;

public enum QueryRowCompiledScenario
{
    NullableNumeric2Full,
    NullableNumeric8Full,
    NullableNumeric32Full,
    NullableNumeric64Full,
    NullableString8Full,
    NullableNumeric8Selective,
    NullableString8HighRejection,
    NullableNumeric8Aggregation,
    NullableNumeric8EarlyTake
}

/// <summary>
/// Measures compile-plus-first-run and warmed compiled execution with identical
/// CSV files, table metadata, SQL semantics, and result oracles.
/// </summary>
[MemoryDiagnoser]
public class QueryScopedCompiledExecutionBenchmark
{
    private const int Rows = 512;
    private static readonly CompilationOptions Options = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(usePrimitiveTypeValidation: false));

    private readonly BenchmarkLoggerResolver _loggerResolver = new();
    private CompiledQuery _legacy = null!;
    private CompiledQuery _queryScoped = null!;
    private QueryScenarioDefinition _definition = null!;
    private string _path = string.Empty;
    private string _query = string.Empty;

    [ParamsAllValues]
    public QueryRowCompiledScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _definition = QueryScenarioDefinition.Create(Scenario);
        _path = Path.Combine(
            Path.GetTempPath(),
            $"musoq-query-row-compiled-{Scenario}-{Guid.NewGuid():N}.csv");
        File.WriteAllText(_path, CreateCsvContent(_definition), new UTF8Encoding(false));
        _query = BuildQuery(_definition, _path, coldToken: null);
        _legacy = Compile(_query, enableQueryScopedRows: false);
        _queryScoped = Compile(_query, enableQueryScopedRows: true);

        var legacy = Execute(_legacy);
        var queryScoped = Execute(_queryScoped);
        if (legacy != queryScoped)
        {
            throw new InvalidOperationException(
                $"Compiled query-row correctness oracle failed for {Scenario}: " +
                $"legacy={legacy}, queryScoped={queryScoped}.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Benchmark(Baseline = true, Description = "Legacy warm execution")]
    public long LegacyWarmExecution() => Execute(_legacy).Consumer;

    [Benchmark(Description = "Query-scoped warm execution")]
    public long QueryScopedWarmExecution() => Execute(_queryScoped).Consumer;

    [Benchmark(Description = "Legacy cold compile and first run")]
    [InvocationCount(1)]
    public long LegacyColdCompileAndFirstRun()
    {
        var query = BuildQuery(_definition, _path, Guid.NewGuid().ToString("N"));
        return Execute(Compile(query, enableQueryScopedRows: false)).Consumer;
    }

    [Benchmark(Description = "Query-scoped cold compile and first run")]
    [InvocationCount(1)]
    public long QueryScopedColdCompileAndFirstRun()
    {
        var query = BuildQuery(_definition, _path, Guid.NewGuid().ToString("N"));
        return Execute(Compile(query, enableQueryScopedRows: true)).Consumer;
    }

    private CompiledQuery Compile(string query, bool enableQueryScopedRows)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString("N"),
            new CsvSchemaProvider(enableQueryScopedRows: enableQueryScopedRows),
            _loggerResolver,
            Options);
    }

    private static CompiledOutcome Execute(CompiledQuery query)
    {
        using var table = query.Run();
        return CompiledOutcome.Create(table);
    }

    private static string BuildQuery(
        QueryScenarioDefinition definition,
        string path,
        string? coldToken)
    {
        var type = definition.Numeric ? "int?" : "string";
        var columns = Enumerable.Range(0, definition.FieldCount)
            .Select(index => $"Column{index}: {type}");
        var allProjection = string.Join(", ", Enumerable.Range(0, definition.FieldCount)
            .Select(index => $"r.Column{index}"));
        var projection = definition.Workload switch
        {
            QueryScenarioWorkload.Selective => "r.Column0, r.Column1",
            QueryScenarioWorkload.HighRejection => "r.Column0",
            QueryScenarioWorkload.Aggregation => "Sum(r.Column0) as Total",
            QueryScenarioWorkload.EarlyTake => "r.Column0, r.Column1",
            _ => allProjection
        };
        var predicate = definition.Workload == QueryScenarioWorkload.HighRejection
            ? "r.Column0 = 'value-499-0'"
            : null;
        if (coldToken != null)
        {
            var tautology = $"'{coldToken}' = '{coldToken}'";
            predicate = predicate == null ? tautology : $"{predicate} and {tautology}";
        }

        var where = predicate == null ? string.Empty : $" where {predicate}";
        var take = definition.Workload == QueryScenarioWorkload.EarlyTake ? " take 16" : string.Empty;
        var sqlPath = path.Replace('\\', '/').Replace("'", "''", StringComparison.Ordinal);
        return
            $"table CsvShape {{ {string.Join(", ", columns)} }};" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select {projection} from Rows('{sqlPath}', true) r{where}{take}";
    }

    private static string CreateCsvContent(QueryScenarioDefinition definition)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Enumerable.Range(0, definition.FieldCount)
            .Select(index => $"Column{index}")));
        for (var row = 0; row < Rows; row++)
        {
            var writtenFields = !definition.Numeric && row % 13 == 0
                ? definition.FieldCount - 1
                : definition.FieldCount;
            for (var column = 0; column < writtenFields; column++)
            {
                if (column > 0)
                    builder.Append(',');

                if (definition.Numeric)
                {
                    if (column == 0 || (row + column) % 17 != 0)
                    {
                        builder.Append(
                            (row * definition.FieldCount + column)
                            .ToString(CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    builder.Append("value-");
                    builder.Append(row.ToString(CultureInfo.InvariantCulture));
                    builder.Append('-');
                    builder.Append(column.ToString(CultureInfo.InvariantCulture));
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private enum QueryScenarioWorkload
    {
        Full,
        Selective,
        HighRejection,
        Aggregation,
        EarlyTake
    }

    private sealed record QueryScenarioDefinition(
        int FieldCount,
        bool Numeric,
        QueryScenarioWorkload Workload)
    {
        public static QueryScenarioDefinition Create(QueryRowCompiledScenario scenario)
        {
            return scenario switch
            {
                QueryRowCompiledScenario.NullableNumeric2Full => new(2, true, QueryScenarioWorkload.Full),
                QueryRowCompiledScenario.NullableNumeric8Full => new(8, true, QueryScenarioWorkload.Full),
                QueryRowCompiledScenario.NullableNumeric32Full => new(32, true, QueryScenarioWorkload.Full),
                QueryRowCompiledScenario.NullableNumeric64Full => new(64, true, QueryScenarioWorkload.Full),
                QueryRowCompiledScenario.NullableString8Full => new(8, false, QueryScenarioWorkload.Full),
                QueryRowCompiledScenario.NullableNumeric8Selective => new(8, true, QueryScenarioWorkload.Selective),
                QueryRowCompiledScenario.NullableString8HighRejection => new(8, false, QueryScenarioWorkload.HighRejection),
                QueryRowCompiledScenario.NullableNumeric8Aggregation => new(8, true, QueryScenarioWorkload.Aggregation),
                QueryRowCompiledScenario.NullableNumeric8EarlyTake => new(8, true, QueryScenarioWorkload.EarlyTake),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
            };
        }
    }

    private readonly record struct CompiledOutcome(int RowCount, long Checksum, ulong OrderHash)
    {
        public long Consumer => unchecked(Checksum + RowCount + (long)OrderHash);

        public static CompiledOutcome Create(Table table)
        {
            var checksum = 0L;
            var orderHash = 0UL;
            foreach (var row in table)
            {
                var rowChecksum = 0L;
                for (var index = 0; index < row.Count; index++)
                    rowChecksum = unchecked(rowChecksum * 397 + ValueChecksum(row[index]));
                checksum = unchecked(checksum + rowChecksum);
                orderHash = unchecked((orderHash ^ (ulong)rowChecksum) * 1099511628211UL);
            }

            return new CompiledOutcome(table.Count, checksum, orderHash);
        }

        private static long ValueChecksum(object? value)
        {
            return value switch
            {
                null => 0,
                int number => number,
                long number => number,
                string text => StringChecksum(text),
                IFormattable formattable => StringChecksum(
                    formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty),
                _ => StringChecksum(value.ToString() ?? string.Empty)
            };
        }

        private static long StringChecksum(string value)
        {
            var checksum = 17L;
            foreach (var character in value)
                checksum = unchecked(checksum * 31 + character);
            return checksum;
        }
    }
}
