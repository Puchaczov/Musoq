using System.Diagnostics;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Playground.Brc;

// One Billion Row Challenge (1BRC) experiment harness.
//
// Workload: aggregate min/mean/max temperature grouped by station name.
// The official challenge uses 413 unique station names and 1,000,000,000 rows.
//
// This harness measures Musoq's end-to-end throughput at smaller scales using the
// engine's typical typed-entity row source path (EntitySource<T>), then extrapolates
// to one billion rows. It also compares parallel vs. serial aggregation.

public sealed class BrcEntity
{
    public BrcEntity(string station, double temperature)
    {
        Station = station;
        Temperature = temperature;
    }

    public string Station { get; }

    public double Temperature { get; }
}

internal sealed class BrcEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(BrcEntity.Station), 0, typeof(string)),
        new SchemaColumn(nameof(BrcEntity.Temperature), 1, typeof(double))
    ];

    public ISchemaColumn? GetColumnByName(string name)
        => Array.Find(Columns, c => c.ColumnName == name);

    public ISchemaColumn[] GetColumnsByName(string name)
        => Columns.Where(c => c.ColumnName == name).ToArray();

    public SchemaTableMetadata Metadata { get; } = new(typeof(BrcEntity));
}

internal sealed class BrcMaterializedSource : RowSource<BrcEntity>
{
    private const int ChunkSize = 4096;
    private readonly IReadOnlyList<BrcEntity> _rows;

    public BrcMaterializedSource(IReadOnlyList<BrcEntity> rows) => _rows = rows;

    public override IEnumerable<IReadOnlyList<BrcEntity>> Chunks
    {
        get
        {
            for (var offset = 0; offset < _rows.Count; offset += ChunkSize)
                yield return new RowChunk<BrcEntity>(_rows, offset, Math.Min(ChunkSize, _rows.Count - offset));
        }
    }
}

internal sealed class BrcStreamingSource : RowSource<BrcEntity>
{
    private readonly long _rowCount;
    private readonly string[] _stations;

    public BrcStreamingSource(long rowCount, string[] stations)
    {
        _rowCount = rowCount;
        _stations = stations;
    }

    public override IEnumerable<IReadOnlyList<BrcEntity>> Chunks
    {
        get
        {
            var random = new Random(12345);
            var stationCount = _stations.Length;
            var chunk = new List<BrcEntity>(4096);
            for (long i = 0; i < _rowCount; i++)
            {
                var station = _stations[random.Next(stationCount)];
                var temperature = Math.Round(random.NextDouble() * 199.8 - 99.9, 1);
                chunk.Add(new BrcEntity(station, temperature));
                if (chunk.Count != 4096)
                    continue;

                yield return chunk;
                chunk = new List<BrcEntity>(4096);
            }

            if (chunk.Count > 0)
                yield return chunk;
        }
    }
}

internal sealed class BrcSchema : SchemaBase
{
    private readonly RowSource<BrcEntity> _source;

    public BrcSchema(RowSource<BrcEntity> source) : base("brc", CreateLibrary())
        => _source = source;

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
        params object?[] parameters)
        => new BrcEntityTable();

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext,
        params object?[] parameters)
        => EnsureSourceType<T, BrcEntity>(name, _source);

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        methodManager.RegisterLibraries(new Library());
        return new MethodsAggregator(methodManager);
    }
}

internal sealed class BrcSchemaProvider : ISchemaProvider
{
    private readonly RowSource<BrcEntity> _source;

    public BrcSchemaProvider(RowSource<BrcEntity> source) => _source = source;

    public ISchema GetSchema(string schema) => new BrcSchema(_source);
}

internal static class BrcExperiment
{
    private const string Query =
        "select Station, Min(Temperature), Avg(Temperature), Max(Temperature) " +
        "from #brc.entities() group by Station";

    private const int StationCount = 413;

    public static void Run()
    {
        Console.WriteLine("=== Musoq One Billion Row Challenge Experiment ===");
        Console.WriteLine($"Cores: {Environment.ProcessorCount}");
        Console.WriteLine($"Query: {Query}");
        Console.WriteLine($"Stations: {StationCount}");
        Console.WriteLine();

        var stations = BuildStations(StationCount);

        if (Environment.GetEnvironmentVariable("BRC_INSPECT") == "1")
        {
            DumpInspection(stations);
            return;
        }

        // Materialized scales (parallel aggregation eligible). Memory ~ 32 bytes/row.
        long[] materializedScales = [2_000_000, 10_000_000, 20_000_000];

        Console.WriteLine("--- Materialized source (in-memory List, parallel-eligible) ---");
        Console.WriteLine($"{"Rows",12} {"Mode",10} {"Time(ms)",10} {"Rows/sec",15} {"Groups",8}");
        foreach (var scale in materializedScales)
        {
            var data = BuildData(scale, stations);
            RunOnce(data, ParallelizationMode.Full, scale);
            RunOnce(data, ParallelizationMode.None, scale);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine();
        Console.WriteLine("--- Streaming source (lazy generator, no retention) ---");
        Console.WriteLine($"{"Rows",12} {"Mode",10} {"Time(ms)",10} {"Rows/sec",15} {"Groups",8}");
        long[] streamingScales = [10_000_000, 50_000_000];
        foreach (var scale in streamingScales)
        {
            RunStreaming(scale, stations, ParallelizationMode.Full);
            RunStreaming(scale, stations, ParallelizationMode.None);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static void RunOnce(IReadOnlyList<BrcEntity> data, ParallelizationMode mode, long scale)
    {
        var source = new BrcMaterializedSource(data);
        var provider = new BrcSchemaProvider(source);
        var options = new CompilationOptions(mode);

        var query = InstanceCreator.CompileForExecution(
            Query, Guid.NewGuid().ToString(), provider, new MyLoggerResolver(), options);

        // Warm up JIT for the generated assembly with a single run is not possible
        // (single source enumeration), so we measure the cold run directly. The query
        // is recompiled per run, so compilation cost is excluded by timing only Run().
        var sw = Stopwatch.StartNew();
        var result = query.Run();
        sw.Stop();

        Report(scale, mode, sw.Elapsed.TotalMilliseconds, result.Count);
    }

    private static void RunStreaming(long scale, string[] stations, ParallelizationMode mode)
    {
        var source = new BrcStreamingSource(scale, stations);
        var provider = new BrcSchemaProvider(source);
        var options = new CompilationOptions(mode);

        var query = InstanceCreator.CompileForExecution(
            Query, Guid.NewGuid().ToString(), provider, new MyLoggerResolver(), options);

        var sw = Stopwatch.StartNew();
        var result = query.Run();
        sw.Stop();

        Report(scale, mode, sw.Elapsed.TotalMilliseconds, result.Count);
    }

    private static void Report(long scale, ParallelizationMode mode, double ms, int groups)
    {
        var rowsPerSec = scale / (ms / 1000.0);
        Console.WriteLine($"{scale,12:N0} {mode,10} {ms,10:N1} {rowsPerSec,15:N0} {groups,8}");
    }

    private static void DumpInspection(string[] stations)
    {
        var data = BuildData(1000, stations);
        var source = new BrcMaterializedSource(data);
        var provider = new BrcSchemaProvider(source);
        var options = new CompilationOptions(ParallelizationMode.Full);

        var inspection = InstanceCreator.CompileForInspection(
            Query, Guid.NewGuid().ToString(), provider, new MyLoggerResolver(), options);

        Console.WriteLine("===== PLANNING TEXT =====");
        Console.WriteLine(inspection.PlanningText);
        Console.WriteLine("===== EXECUTION PLAN =====");
        Console.WriteLine(inspection.ExecutionPlanText);
        Console.WriteLine("===== GENERATED C# =====");
        Console.WriteLine(inspection.GeneratedCSharpCode);
    }

    private static string[] BuildStations(int count)
    {
        var stations = new string[count];
        for (var i = 0; i < count; i++)
            stations[i] = $"Station_{i:D4}";
        return stations;
    }

    private static IReadOnlyList<BrcEntity> BuildData(long rowCount, string[] stations)
    {
        var random = new Random(12345);
        var data = new BrcEntity[rowCount];
        var stationCount = stations.Length;
        for (long i = 0; i < rowCount; i++)
        {
            var station = stations[random.Next(stationCount)];
            var temperature = Math.Round(random.NextDouble() * 199.8 - 99.9, 1);
            data[i] = new BrcEntity(station, temperature);
        }

        return data;
    }
}
