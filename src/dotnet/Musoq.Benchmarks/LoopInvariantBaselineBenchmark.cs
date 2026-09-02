using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
/// Establishes the nested APPLY recomputation baseline before loop-invariant code motion is introduced.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class LoopInvariantBaselineBenchmark
{
    private const string Query = """
        select i.ExpensiveValue + n.Value + m.Value
        from #loop.items() i
        cross apply i.Numbers n
        cross apply i.Numbers m
        """;

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private IReadOnlyList<LoopInvariantBaselineRow> _rows = null!;
    private CompiledQuery _generatedQuery = null!;

    [Params(4, 16)]
    public int OuterRows { get; set; }

    [Params(4, 8)]
    public int Fanout { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = Enumerable.Range(0, OuterRows)
            .Select(index => new LoopInvariantBaselineRow
            {
                Id = index,
                Numbers = Enumerable.Range(1, Fanout).ToArray()
            })
            .ToArray();

        _generatedQuery = InstanceCreator.CompileForExecution(
            Query,
            Guid.NewGuid().ToString(),
            new LoopInvariantBaselineSchemaProvider(_rows),
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized(
                new CompilationOptions(ParallelizationMode.None).WithLoopInvariantCodeMotion(false)));
    }

    [Benchmark(Baseline = true)]
    public long GeneratedQuery()
    {
        var table = _generatedQuery.Run();
        long checksum = 0;

        foreach (var row in table.Rows)
            checksum += Convert.ToInt64(row[0]);

        return checksum;
    }

    [Benchmark]
    public long HandHoistedReference()
    {
        long checksum = 0;

        foreach (var row in _rows)
        {
            var value = row.ExpensiveValue;

            foreach (var first in row.Numbers)
            foreach (var second in row.Numbers)
                checksum += value + first + second;
        }

        return checksum;
    }

    public sealed class LoopInvariantBaselineRow
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Id)] = 0,
                [nameof(ExpensiveValue)] = 1,
                [nameof(Numbers)] = 2
            };

        public static readonly IReadOnlyDictionary<int, Func<LoopInvariantBaselineRow, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<LoopInvariantBaselineRow, object?>>
            {
                [0] = row => row.Id,
                [1] = row => row.ExpensiveValue,
                [2] = row => row.Numbers
            };

        public int Id { get; init; }

        public int[] Numbers { get; init; } = [];

        public int ExpensiveValue => ComputeExpensiveValue(Id);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ComputeExpensiveValue(int value)
        {
            var result = value + 17;

            for (var index = 0; index < 24; index++)
                result = unchecked(result * 31 + index);

            return result;
        }
    }

    private sealed class LoopInvariantBaselineSchemaProvider(IReadOnlyList<LoopInvariantBaselineRow> rows)
        : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new LoopInvariantBaselineSchema(rows);
        }
    }

    private sealed class LoopInvariantBaselineSchema(IReadOnlyList<LoopInvariantBaselineRow> rows)
        : SchemaBase("loop", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return new LoopInvariantBaselineTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, LoopInvariantBaselineRow>(name, new LoopInvariantBaselineRowSource(rows));

            throw new NotSupportedException(name);
        }
    }

    private sealed class LoopInvariantBaselineTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(LoopInvariantBaselineRow.Id), 0, typeof(int)),
            new SchemaColumn(nameof(LoopInvariantBaselineRow.ExpensiveValue), 1, typeof(int)),
            new SchemaColumn(nameof(LoopInvariantBaselineRow.Numbers), 2, typeof(int[]))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(LoopInvariantBaselineRow));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class LoopInvariantBaselineRowSource(IReadOnlyList<LoopInvariantBaselineRow> rows)
        : RowSourceBase<LoopInvariantBaselineRow>
    {
        protected override void CollectChunks(IChunkWriter<LoopInvariantBaselineRow> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new Library());
        return new MethodsAggregator(methodsManager);
    }

    private sealed class Library : LibraryBase;
}
