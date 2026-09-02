using System.Collections.Generic;
using System.Linq;
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
/// Measures equivalent generated queries with loop-invariant code motion enabled and disabled.
/// The benchmark validates row and evaluation-count oracles on every invocation so a timing
/// result cannot hide a semantic change.
/// </summary>
[InProcess]
[WarmupCount(3)]
[IterationCount(3)]
[MemoryDiagnoser]
public class LoopInvariantQualificationBenchmark
{
    private const int OuterRowCount = 2;
    private const int QueryRepetitions = 64;
    private const string QueryTemplate = "select {0} + n.Value + m.Value from #loopq.items() i cross apply i.Numbers n cross apply i.Numbers m";
    private const string VolatileQueryTemplate = "select {0} from #loopq.items() i cross apply i.Numbers n cross apply i.Numbers m";

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private IReadOnlyList<QualificationRow> _rows = null!;
    private Dictionary<(QualificationScenario Scenario, bool LicmEnabled), CompiledQuery> _compiled = null!;

    [Params(1, 8, 64)]
    public int Fanout { get; set; }

    [Params(
        QualificationScenario.StableCheapGetter,
        QualificationScenario.StableExpensiveGetter,
        QualificationScenario.VolatileGetter,
        QualificationScenario.StableCheapCallable,
        QualificationScenario.StableExpensiveCallable,
        QualificationScenario.VolatileCallable)]
    public QualificationScenario Scenario { get; set; }

    // This property is retained for the direct oracle test. BenchmarkDotNet uses
    // ExecuteOff and ExecuteOn below so both methods are measured in one process
    // for each fan-out/scenario pair.
    public bool LicmEnabled { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = Enumerable.Range(1, OuterRowCount)
            .Select(id => new QualificationRow
            {
                Id = id,
                Numbers = Enumerable.Range(1, Fanout).ToArray()
            })
            .ToArray();

        _compiled = new Dictionary<(QualificationScenario Scenario, bool LicmEnabled), CompiledQuery>();
        foreach (var scenario in Enum.GetValues<QualificationScenario>())
        foreach (var enabled in new[] { false, true })
        {
            var options = BenchmarkCompilationOptions.Materialized(
                new CompilationOptions(
                    ParallelizationMode.None,
                    useCommonSubexpressionElimination: false)
                .WithLoopInvariantCodeMotion(enabled));
            _compiled.Add(
                (scenario, enabled),
                InstanceCreator.CompileForExecution(
                    scenario.IsVolatile()
                        ? string.Format(VolatileQueryTemplate, scenario.Expression())
                        : string.Format(QueryTemplate, scenario.QualificationExpression()),
                    $"LoopInvariantQualification_{scenario}_{enabled}_{Fanout}",
                    new QualificationSchemaProvider(_rows),
                    _loggerResolver,
                    options));
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

    public long Execute() => ExecuteCore(LicmEnabled);

    private long ExecuteCore(bool enabled)
    {
        QualificationCounters.Reset();
        long checksum = 0;
        var expectedRows = OuterRowCount * Fanout * Fanout;
        for (var repetition = 0; repetition < QueryRepetitions; repetition++)
        {
            using var table = _compiled[(Scenario, enabled)].Run();
            if (table.Count != expectedRows)
                throw new InvalidOperationException($"Expected {expectedRows} rows, received {table.Count}.");

            foreach (var row in table.Rows)
                checksum += Convert.ToInt64(row[0]);
        }

        var expectedProducerReads = QueryRepetitions * ExpectedProducerReads(enabled);
        var actualProducerReads = QualificationCounters.ProducerReads;
        if (actualProducerReads != expectedProducerReads)
        {
            throw new InvalidOperationException(
                $"{Scenario}/{enabled} expected {expectedProducerReads} producer reads, " +
                $"received {actualProducerReads}.");
        }

        return checksum;
    }

    private int ExpectedProducerReads(bool enabled)
    {
        var volatileProducer = Scenario is QualificationScenario.VolatileGetter or QualificationScenario.VolatileCallable;
        var producerReferences = Scenario is QualificationScenario.StableCheapCallable or QualificationScenario.StableExpensiveCallable
            ? 2
            : 1;
        return producerReferences * (volatileProducer || !enabled
            ? OuterRowCount * Fanout * Fanout
            : OuterRowCount);
    }

    public enum QualificationScenario
    {
        StableCheapGetter,
        StableExpensiveGetter,
        VolatileGetter,
        StableCheapCallable,
        StableExpensiveCallable,
        VolatileCallable
    }

    public sealed class QualificationRow
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Id)] = 0,
                [nameof(CheapValue)] = 1,
                [nameof(ExpensiveValue)] = 2,
                [nameof(VolatileValue)] = 3,
                [nameof(Numbers)] = 4
            };

        public static readonly IReadOnlyDictionary<int, Func<QualificationRow, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<QualificationRow, object?>>
            {
                [0] = row => row.Id,
                [1] = row => row.CheapValue,
                [2] = row => row.ExpensiveValue,
                [3] = row => row.VolatileValue,
                [4] = row => row.Numbers
            };

        public int Id { get; init; }

        public int[] Numbers { get; init; } = [];

        public int CheapValue => QualificationCounters.ReadCheap(Id);

        public int ExpensiveValue => QualificationCounters.ReadExpensive(Id);

        [NonDeterministic]
        public int VolatileValue => QualificationCounters.ReadVolatile(Id);
    }

    public sealed class QualificationSchemaProvider(IReadOnlyList<QualificationRow> rows) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new QualificationSchema(rows);
    }

    public sealed class QualificationSchema(IReadOnlyList<QualificationRow> rows)
        : SchemaBase("loopq", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return new QualificationTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, QualificationRow>(name, new QualificationRowSource(rows));

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new QualificationLibrary());
            return new MethodsAggregator(methodsManager);
        }
    }

    public sealed class QualificationTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new Musoq.Schema.DataSources.SchemaColumn(nameof(QualificationRow.Id), 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(QualificationRow.CheapValue), 1, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(QualificationRow.ExpensiveValue), 2, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(QualificationRow.VolatileValue), 3, typeof(int), (string?)null, ColumnStability.Volatile),
            new Musoq.Schema.DataSources.SchemaColumn(nameof(QualificationRow.Numbers), 4, typeof(int[]))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(QualificationRow));

        public ISchemaColumn GetColumnByName(string name) =>
            Columns.Single(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public sealed class QualificationRowSource(IReadOnlyList<QualificationRow> rows)
        : RowSourceBase<QualificationRow>
    {
        protected override void CollectChunks(IChunkWriter<QualificationRow> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

    public sealed class QualificationLibrary : LibraryBase
    {
        [BindableMethod]
        public int StableCheap(int value) => QualificationCounters.ReadCheapCallable(value);

        [BindableMethod]
        public int StableExpensive(int value) => QualificationCounters.ReadExpensiveCallable(value);

        [BindableMethod]
        [NonDeterministic]
        public int VolatileCallable(int value) => QualificationCounters.ReadVolatileCallable(value);
    }

    public static class QualificationCounters
    {
        private static int _producerReads;

        public static int ProducerReads => Volatile.Read(ref _producerReads);

        public static void Reset() => Interlocked.Exchange(ref _producerReads, 0);

        public static int ReadCheap(int value)
        {
            Interlocked.Increment(ref _producerReads);
            return value + 1;
        }

        public static int ReadExpensive(int value)
        {
            Interlocked.Increment(ref _producerReads);
            return Expensive(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReadVolatile(int value)
        {
            Interlocked.Increment(ref _producerReads);
            return VolatileWork(value, 17);
        }

        public static int ReadCheapCallable(int value)
        {
            Interlocked.Increment(ref _producerReads);
            return value + 5;
        }

        public static int ReadExpensiveCallable(int value)
        {
            Interlocked.Increment(ref _producerReads);
            return Expensive(value + 7);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReadVolatileCallable(int value)
        {
            Interlocked.Increment(ref _producerReads);
            return VolatileWork(value, 23);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Expensive(int value)
        {
            var result = value;
            for (var index = 0; index < 64; index++)
                result = unchecked(result * 31 + index);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int VolatileWork(int value, int seed)
        {
            var result = value + seed;
            for (var index = 0; index < 512; index++)
                result = unchecked(result * 17 + index);
            return result;
        }
    }
}

internal static class QualificationScenarioExtensions
{
    public static bool IsVolatile(this LoopInvariantQualificationBenchmark.QualificationScenario scenario) =>
        scenario is LoopInvariantQualificationBenchmark.QualificationScenario.VolatileGetter or
            LoopInvariantQualificationBenchmark.QualificationScenario.VolatileCallable;

    public static string Expression(this LoopInvariantQualificationBenchmark.QualificationScenario scenario) =>
        scenario switch
        {
            LoopInvariantQualificationBenchmark.QualificationScenario.StableCheapGetter => "i.CheapValue",
            LoopInvariantQualificationBenchmark.QualificationScenario.StableExpensiveGetter => "i.ExpensiveValue",
            LoopInvariantQualificationBenchmark.QualificationScenario.VolatileGetter => "i.VolatileValue",
            LoopInvariantQualificationBenchmark.QualificationScenario.StableCheapCallable => "i.StableCheap(i.Id)",
            LoopInvariantQualificationBenchmark.QualificationScenario.StableExpensiveCallable => "i.StableExpensive(i.Id)",
            LoopInvariantQualificationBenchmark.QualificationScenario.VolatileCallable => "i.VolatileCallable(i.Id)",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

    public static string QualificationExpression(this LoopInvariantQualificationBenchmark.QualificationScenario scenario)
    {
        var expression = scenario.Expression();
        return scenario is LoopInvariantQualificationBenchmark.QualificationScenario.StableCheapCallable or
            LoopInvariantQualificationBenchmark.QualificationScenario.StableExpensiveCallable
            ? $"({expression}) + ({expression})"
            : expression;
    }
}
