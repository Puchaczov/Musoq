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

[ShortRunJob]
[MemoryDiagnoser]
public class WeatherAggregateBenchmark
{
    private const string Query =
        "select City, Min(Temperature::Single), Max(Temperature::Single), Avg(Temperature::Single) " +
        "from #weather.measurements() group by City";

    private const int CityCardinality = 413;

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _parallelQuery = null!;
    private CompiledQuery _serialQuery = null!;
    private WeatherAggregateRow[] _rows = null!;

    [Params(100_000, 1_000_000)]
    public int RowCount { get; set; }

    [Params(512, 4096)]
    public int ChunkSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = CreateRows(RowCount, CityCardinality);

        _serialQuery = Compile(new CompilationOptions(
            parallelizationMode: ParallelizationMode.None));
        _parallelQuery = Compile(new CompilationOptions(
            parallelizationMode: ParallelizationMode.Full));
    }

    [Benchmark(Baseline = true)]
    public int SerialGroupedAggregate()
    {
        return ConsumeRows(_serialQuery.Run(CancellationToken.None));
    }

    [Benchmark]
    public int ParallelGroupedAggregate()
    {
        return ConsumeRows(_parallelQuery.Run(CancellationToken.None));
    }

    private CompiledQuery Compile(CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            Query,
            $"MusoqWeatherAggregateBenchmark_{Guid.NewGuid():N}",
            new WeatherAggregateSchemaProvider(_rows, ChunkSize),
            _loggerResolver,
            options);
    }

    private static int ConsumeRows(Table table)
    {
        var count = 0;
        foreach (var _ in table)
            count++;

        return count;
    }

    private static WeatherAggregateRow[] CreateRows(int rowCount, int cityCardinality)
    {
        var cities = Enumerable
            .Range(0, cityCardinality)
            .Select(static index => $"City_{index:D4}")
            .ToArray();
        var rows = new WeatherAggregateRow[rowCount];
        var random = new Random(12345);

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = new WeatherAggregateRow
            {
                City = cities[random.Next(cities.Length)],
                Temperature = Math.Round(random.NextDouble() * 199.8 - 99.9, 1)
            };
        }

        return rows;
    }

    public sealed class WeatherAggregateRow
    {
        public string City { get; init; } = string.Empty;

        public double Temperature { get; init; }
    }

    private sealed class WeatherAggregateSchemaProvider(
        IReadOnlyList<WeatherAggregateRow> rows,
        int chunkSize) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "#weather", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "weather", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(schema);
            }

            return new WeatherAggregateSchema(rows, chunkSize);
        }
    }

    private sealed class WeatherAggregateSchema(
        IReadOnlyList<WeatherAggregateRow> rows,
        int chunkSize) : SchemaBase("weather", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "measurements", StringComparison.OrdinalIgnoreCase))
                return new WeatherAggregateTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "measurements", StringComparison.OrdinalIgnoreCase))
            {
                return EnsureSourceType<T, WeatherAggregateRow>(
                    name,
                    new WeatherAggregateRowSource(rows, chunkSize));
            }

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class WeatherAggregateTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(WeatherAggregateRow.City), 0, typeof(string)),
            new SchemaColumn(nameof(WeatherAggregateRow.Temperature), 1, typeof(double))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(WeatherAggregateRow));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class WeatherAggregateRowSource(
        IReadOnlyList<WeatherAggregateRow> rows,
        int chunkSize) : RowSourceBase<WeatherAggregateRow>
    {
        protected override void CollectChunks(IChunkWriter<WeatherAggregateRow> writer)
        {
            for (var index = 0; index < rows.Count; index += chunkSize)
            {
                var count = Math.Min(chunkSize, rows.Count - index);
                var chunk = new WeatherAggregateRow[count];
                for (var chunkIndex = 0; chunkIndex < count; chunkIndex++)
                    chunk[chunkIndex] = rows[index + chunkIndex];

                writer.Write(chunk);
            }
        }
    }

    private sealed class Library : LibraryBase;
}
