using System.Threading;
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

[ShortRunJob]
[MemoryDiagnoser]
public class StreamingChunkParallelProjectionBenchmark
{
    private const string Query =
        "select ExpensiveCompute(Value) from #bench.rows() where ExpensivePredicate(Value)";

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _serialQuery = null!;
    private CompiledQuery _parallelQuery = null!;
    private StreamingBenchmarkRow[] _rows = null!;

    [Params(100_000, 1_000_000)]
    public int RowCount { get; set; }

    [Params(512, 4096)]
    public int ChunkSize { get; set; }

    [Params(2, 4, 8)]
    public int MaxDegree { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = Enumerable.Range(0, RowCount)
            .Select(static value => new StreamingBenchmarkRow { Value = value })
            .ToArray();

        _serialQuery = Compile(new CompilationOptions(
            ParallelizationMode.None,
            maxDegreeOfParallelismOverride: MaxDegree));
        _parallelQuery = Compile(new CompilationOptions(
            ParallelizationMode.Full,
            maxDegreeOfParallelismOverride: MaxDegree));
    }

    [Benchmark(Baseline = true)]
    public int SerialStreamingProjection()
    {
        return _serialQuery.Run(CancellationToken.None).Count;
    }

    [Benchmark]
    public int ChunkParallelStreamingProjection()
    {
        return _parallelQuery.Run(CancellationToken.None).Count;
    }

    private CompiledQuery Compile(CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            Query,
            $"MusoqStreamingChunkParallelProjectionBenchmark_{Guid.NewGuid():N}",
            new StreamingBenchmarkSchemaProvider(_rows, ChunkSize),
            _loggerResolver,
            options);
    }

    public sealed class StreamingBenchmarkRow
    {
        public int Value { get; init; }
    }

    private sealed class StreamingBenchmarkSchemaProvider(
        IReadOnlyList<StreamingBenchmarkRow> rows,
        int chunkSize) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "#bench", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "bench", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(schema);
            }

            return new StreamingBenchmarkSchema(rows, chunkSize);
        }
    }

    private sealed class StreamingBenchmarkSchema(
        IReadOnlyList<StreamingBenchmarkRow> rows,
        int chunkSize) : SchemaBase("bench", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
                return new StreamingBenchmarkTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
            {
                return EnsureSourceType<T, StreamingBenchmarkRow>(
                    name,
                    new StreamingBenchmarkRowSource(rows, chunkSize));
            }

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new StreamingBenchmarkLibrary());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class StreamingBenchmarkTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(StreamingBenchmarkRow.Value), 0, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(StreamingBenchmarkRow));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class StreamingBenchmarkRowSource(
        IReadOnlyList<StreamingBenchmarkRow> rows,
        int chunkSize) : RowSourceBase<StreamingBenchmarkRow>
    {
        protected override void CollectChunks(IChunkWriter<StreamingBenchmarkRow> writer)
        {
            for (var index = 0; index < rows.Count; index += chunkSize)
            {
                var count = Math.Min(chunkSize, rows.Count - index);
                var chunk = new StreamingBenchmarkRow[count];
                for (var chunkIndex = 0; chunkIndex < count; chunkIndex++)
                    chunk[chunkIndex] = rows[index + chunkIndex];

                writer.Write(chunk);
            }
        }
    }

    public sealed class StreamingBenchmarkLibrary : LibraryBase
    {
        [BindableMethod]
        public bool ExpensivePredicate(int value)
        {
            return (Mix(value) & 3) != 0;
        }

        [BindableMethod]
        public int ExpensiveCompute(int value)
        {
            return Mix(value ^ unchecked((int)0x9E3779B9));
        }

        private static int Mix(int value)
        {
            unchecked
            {
                var state = (uint)value;
                for (var iteration = 0; iteration < 96; iteration++)
                {
                    state ^= state << 13;
                    state ^= state >> 17;
                    state ^= state << 5;
                    state += 0x9E3779B9u + (uint)iteration;
                }

                return (int)state;
            }
        }
    }
}
