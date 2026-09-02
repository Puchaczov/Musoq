using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
/// Measures the cross-operator stability-aware scalar reuse path independently
/// from serial loop-invariant code motion and ordinary CSE.
/// </summary>
[InProcess]
[WarmupCount(5)]
[IterationCount(5)]
[MemoryDiagnoser]
public class StabilityAwareScalarReuseQualificationBenchmark
{
    private const int OuterRowCount = 8;
    private const string FilterQueryTemplate =
        "select {0} from #reuse.items() i where {0} > 0 order by i.Id";
    private const string AggregateQueryTemplate =
        "select i.ExpensiveValue, Count(i.ExpensiveValue) from #reuse.items() i group by i.ExpensiveValue";

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private IReadOnlyList<ReuseRow> _rows = null!;
    private Dictionary<(QualificationScenario Scenario, bool Enabled), CompiledQuery> _compiled = null!;
    private long _expectedFilterChecksum;
    private long _expectedAggregateChecksum;

    [Params(1, 8, 64)]
    public int Fanout { get; set; }

    [Params(
        QualificationScenario.StableCheapFilter,
        QualificationScenario.StableExpensiveFilter,
        QualificationScenario.StableAggregate,
        QualificationScenario.VolatileFilter)]
    public QualificationScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var measuredFanout = Math.Max(8, Fanout);
        _rows = Enumerable.Range(1, OuterRowCount * measuredFanout)
            .Select(id => new ReuseRow { Id = id, Group = id % Math.Max(1, measuredFanout / 2) })
            .ToArray();
        _expectedFilterChecksum = Scenario switch
        {
            QualificationScenario.StableExpensiveFilter => _rows.Sum(row => (long)ExpensiveValueFor(row.Id)),
            QualificationScenario.NoCandidate => _rows.Sum(row => (long)row.Id),
            _ => _rows.Sum(row => (long)CheapValueFor(row.Id))
        };
        _expectedAggregateChecksum = _rows
            .Select(row => (long)ExpensiveValueFor(row.Id))
            .Distinct()
            .Sum();

        _compiled = new Dictionary<(QualificationScenario Scenario, bool Enabled), CompiledQuery>();
        foreach (var enabled in new[] { false, true })
        {
            var options = BenchmarkCompilationOptions.Materialized(
                new CompilationOptions(
                    ParallelizationMode.None,
                    useCommonSubexpressionElimination: false)
                .WithLoopInvariantCodeMotion(false)
                .WithStabilityAwareScalarReuse(enabled));

            foreach (var scenario in Enum.GetValues<QualificationScenario>())
            {
                var query = scenario == QualificationScenario.StableAggregate
                    ? AggregateQueryTemplate
                    : string.Format(FilterQueryTemplate, scenario.Expression());
                _compiled.Add(
                    (scenario, enabled),
                    InstanceCreator.CompileForExecution(
                        query,
                        $"StabilityAwareScalarReuse_{scenario}_{enabled}_{Fanout}",
                        new ReuseSchemaProvider(_rows),
                        _loggerResolver,
                        options));
            }
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var query in _compiled.Values)
            query.Dispose();
    }

    [Benchmark(Baseline = true)]
    public long ExecuteOff() => ExecuteCore(enabled: false);

    [Benchmark]
    public long ExecuteOn() => ExecuteCore(enabled: true);

    private long ExecuteCore(bool enabled)
    {
        ReuseCounters.Reset();
        var expectedRows = Scenario == QualificationScenario.StableAggregate
            ? _rows.Select(row => ExpensiveValueFor(row.Id)).Distinct().Count()
            : _rows.Count;
        var expectedChecksum = Scenario == QualificationScenario.StableAggregate
            ? _expectedAggregateChecksum
            : _expectedFilterChecksum;

        using var table = _compiled[(Scenario, enabled)].Run();
        if (table.Count != expectedRows)
            throw new InvalidOperationException($"Expected {expectedRows} rows, received {table.Count}.");

        long checksum = 0;
        foreach (var row in table.Rows)
            checksum += Convert.ToInt64(row[0]);
        if (checksum != expectedChecksum)
            throw new InvalidOperationException($"Expected checksum {expectedChecksum}, received {checksum}.");

        if (Scenario is QualificationScenario.StableCheapFilter or QualificationScenario.StableExpensiveFilter)
        {
            var expectedReads = _rows.Count * (enabled ? 1 : 2);
            if (ReuseCounters.GetterReads != expectedReads)
                throw new InvalidOperationException(
                    $"Expected {expectedReads} stable getter reads, received {ReuseCounters.GetterReads}.");
        }
        else if (Scenario == QualificationScenario.VolatileFilter &&
                 ReuseCounters.GetterReads != _rows.Count * 2)
        {
            throw new InvalidOperationException(
                $"Expected {_rows.Count * 2} volatile getter reads, received {ReuseCounters.GetterReads}.");
        }

        return checksum;
    }

    public enum QualificationScenario
    {
        StableCheapFilter,
        StableExpensiveFilter,
        StableAggregate,
        VolatileFilter,
        NoCandidate
    }

    public sealed class ReuseRow
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Id)] = 0,
                [nameof(Group)] = 1,
                [nameof(CheapValue)] = 2,
                [nameof(ExpensiveValue)] = 3,
                [nameof(VolatileValue)] = 4
            };

        public static readonly IReadOnlyDictionary<int, Func<ReuseRow, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<ReuseRow, object?>>
            {
                [0] = row => row.Id,
                [1] = row => row.Group,
                [2] = row => row.CheapValue,
                [3] = row => row.ExpensiveValue,
                [4] = row => row.VolatileValue
            };

        public int Id { get; init; }

        public int Group { get; init; }

        public int CheapValue => ReuseCounters.ReadCheap(Id);

        public int ExpensiveValue => ReuseCounters.ReadExpensive(Id);

        [NonDeterministic]
        public int VolatileValue => ReuseCounters.ReadVolatile(Id);
    }

    public sealed class ReuseSchemaProvider(IReadOnlyList<ReuseRow> rows) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new ReuseSchema(rows);
    }

    private sealed class ReuseSchema(IReadOnlyList<ReuseRow> rows)
        : SchemaBase("reuse", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return new ReuseTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, ReuseRow>(name, new ReuseRowSource(rows));

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class ReuseTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new Musoq.Schema.DataSources.SchemaColumn(nameof(ReuseRow.Id), 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(ReuseRow.Group), 1, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(ReuseRow.CheapValue), 2, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(ReuseRow.ExpensiveValue), 3, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(ReuseRow.VolatileValue), 4, typeof(int), (string?)null, ColumnStability.Volatile)
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(ReuseRow));

        public ISchemaColumn GetColumnByName(string name) =>
            Columns.Single(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private sealed class ReuseRowSource(IReadOnlyList<ReuseRow> rows) : RowSourceBase<ReuseRow>
    {
        protected override void CollectChunks(IChunkWriter<ReuseRow> writer) => writer.Write(rows.ToArray());
    }

    public static class ReuseCounters
    {
        private static int _getterReads;

        public static int GetterReads => Volatile.Read(ref _getterReads);

        public static void Reset() => Interlocked.Exchange(ref _getterReads, 0);

        public static int ReadCheap(int value)
        {
            Interlocked.Increment(ref _getterReads);
            return CheapValueFor(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReadExpensive(int value)
        {
            Interlocked.Increment(ref _getterReads);
            return ExpensiveValueFor(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReadVolatile(int value)
        {
            Interlocked.Increment(ref _getterReads);
            return CheapValueFor(value);
        }
    }

    private static int ExpensiveValueFor(int value)
    {
        var result = value + 17;
        for (var index = 0; index < 256; index++)
            result = unchecked(result * 31 + index);
        return result & int.MaxValue;
    }

    private static int CheapValueFor(int value)
    {
        var result = value + 17;
        for (var index = 0; index < 32; index++)
            result = unchecked(result * 31 + index);
        return result & int.MaxValue;
    }
}

internal static class StabilityAwareScalarReuseQualificationScenarioExtensions
{
    public static string Expression(
        this StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario scenario) =>
        scenario switch
        {
            StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.StableCheapFilter => "i.CheapValue",
            StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.StableExpensiveFilter => "i.ExpensiveValue",
            StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.VolatileFilter => "i.VolatileValue",
            StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.NoCandidate => "i.Id",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
}
