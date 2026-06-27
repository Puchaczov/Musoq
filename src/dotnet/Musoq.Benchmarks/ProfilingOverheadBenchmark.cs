using System.Reflection;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class ProfilingOverheadBenchmark : BenchmarkBase
{
    public enum ProfilingOverheadScenario
    {
        ScanFilterProject,
        AppendAll,
        HashJoin,
        GroupBy,
        OrderBySkipTake,
        ParallelCteHashJoin
    }

    private CompiledQuery _disabledQuery = null!;
    private ProfiledQueryRunner _sourceBoundariesQuery = null!;
    private ProfiledQueryRunner _fullQuery = null!;
    private QueryProfileSnapshot? _lastProfile;

    [Params(1_000, 10_000)]
    public int RowsCount { get; set; }

    [Params(BenchmarkChunkShape.Chunk512, BenchmarkChunkShape.Chunk4096, BenchmarkChunkShape.SingleGiant)]
    public BenchmarkChunkShape ChunkShape { get; set; }

    [Params(
        ProfilingOverheadScenario.ScanFilterProject,
        ProfilingOverheadScenario.AppendAll,
        ProfilingOverheadScenario.HashJoin,
        ProfilingOverheadScenario.GroupBy,
        ProfilingOverheadScenario.OrderBySkipTake,
        ProfilingOverheadScenario.ParallelCteHashJoin)]
    public ProfilingOverheadScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = CreateScript(Scenario);
        var schemaProvider = CreateSchemaProvider(RowsCount, ChunkShape);
        var options = BenchmarkCompilationOptions.Materialized(
            new CompilationOptions(
                parallelizationMode: ParallelizationMode.Full,
                useCteParallelization: true));

        _disabledQuery = Compile(script, schemaProvider, options);
        _sourceBoundariesQuery = ProfiledQueryRunner.Create(
            Compile(script, schemaProvider, options.WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries)));
        _fullQuery = ProfiledQueryRunner.Create(
            Compile(script, schemaProvider, options.WithInstrumentationMode(QueryInstrumentationMode.Full)));
    }

    [Benchmark(Baseline = true)]
    public Table Disabled_Run()
    {
        return _disabledQuery.Run(TokenSource.Token);
    }

    [Benchmark]
    public Table SourceBoundaries_RunWithRecorder()
    {
        return _sourceBoundariesQuery.Run(TokenSource.Token, out _lastProfile);
    }

    [Benchmark]
    public Table Full_RunWithRecorder()
    {
        return _fullQuery.Run(TokenSource.Token, out _lastProfile);
    }

    private CompiledQuery Compile(
        string script,
        ISchemaProvider schemaProvider,
        CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new BenchmarkLoggerResolver(),
            options);
    }

    private static string CreateScript(ProfilingOverheadScenario scenario)
    {
        return scenario switch
        {
            ProfilingOverheadScenario.ScanFilterProject =>
                "select Id, Name, City, Population from #A.entities() where Population > 50000",
            ProfilingOverheadScenario.AppendAll =>
                "select Id, Name, City, Category, Population from #A.entities()",
            ProfilingOverheadScenario.HashJoin =>
                "select a.Id, a.Name, b.Score from #A.entities() a inner join #B.entities() b on a.Id = b.Id",
            ProfilingOverheadScenario.GroupBy =>
                "select City, Count(City) as CityCount from #A.entities() group by City",
            ProfilingOverheadScenario.OrderBySkipTake =>
                "select Id, Name, City, Population from #A.entities() order by Population desc skip 100 take 100",
            ProfilingOverheadScenario.ParallelCteHashJoin =>
                "with leftCte as (select Id, City from #A.entities()), rightCte as (select Id, Name from #B.entities()) select l.Id, l.City, r.Name from leftCte l inner join rightCte r on l.Id = r.Id",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    private static ISchemaProvider CreateSchemaProvider(int rowsCount, BenchmarkChunkShape chunkShape)
    {
        var leftRows = CreateRows(rowsCount, "left");
        var rightRows = CreateRows(rowsCount, "right");

        return new GenericSchemaProvider<ProfilingOverheadEntity, ProfilingOverheadTable>(
            BenchmarkSourceChunks.FromRows(new Dictionary<string, IEnumerable<ProfilingOverheadEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = leftRows,
                ["#A"] = leftRows,
                ["B"] = rightRows,
                ["#B"] = rightRows
            }, chunkShape),
            ProfilingOverheadEntity.NameToIndexMap,
            ProfilingOverheadEntity.IndexToObjectAccessMap);
    }

    private static ProfilingOverheadEntity[] CreateRows(int count, string prefix)
    {
        return Enumerable.Range(0, count)
            .Select(index => new ProfilingOverheadEntity
            {
                Id = index,
                Name = $"{prefix}_{index}",
                City = $"City_{index % 257}",
                Category = $"Category_{index % 17}",
                Population = 10_000 + (index * 97 % 1_000_000),
                Score = index * 31 % 10_000
            })
            .ToArray();
    }

    private sealed class ProfiledQueryRunner
    {
        private static readonly FieldInfo RunnableField =
            typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new MissingFieldException(typeof(CompiledQuery).FullName, "_runnable");

        private readonly IProfiledRunnable _runnable;

        private ProfiledQueryRunner(IProfiledRunnable runnable)
        {
            _runnable = runnable;
        }

        public static ProfiledQueryRunner Create(CompiledQuery query)
        {
            if (RunnableField.GetValue(query) is not IProfiledRunnable runnable)
                throw new InvalidOperationException("Compiled query does not expose a profiled runnable.");

            return new ProfiledQueryRunner(runnable);
        }

        public Table Run(CancellationToken token, out QueryProfileSnapshot snapshot)
        {
            var recorder = new QueryProfileRecorder();
            var result = _runnable.RunWithProfile(token, recorder);
            snapshot = recorder.CreateSnapshot();
            return result;
        }
    }
}

public sealed class ProfilingOverheadEntity
{
    public static readonly IDictionary<string, int> NameToIndexMap =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Id)] = 0,
            [nameof(Name)] = 1,
            [nameof(City)] = 2,
            [nameof(Category)] = 3,
            [nameof(Population)] = 4,
            [nameof(Score)] = 5
        };

    public static readonly IDictionary<int, Func<ProfilingOverheadEntity, object?>> IndexToObjectAccessMap =
        new Dictionary<int, Func<ProfilingOverheadEntity, object?>>
        {
            [0] = entity => entity.Id,
            [1] = entity => entity.Name,
            [2] = entity => entity.City,
            [3] = entity => entity.Category,
            [4] = entity => entity.Population,
            [5] = entity => entity.Score
        };

    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Population { get; init; }

    public int Score { get; init; }
}

public sealed class ProfilingOverheadTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(ProfilingOverheadEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(ProfilingOverheadEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(ProfilingOverheadEntity.City), 2, typeof(string)),
        new SchemaColumn(nameof(ProfilingOverheadEntity.Category), 3, typeof(string)),
        new SchemaColumn(nameof(ProfilingOverheadEntity.Population), 4, typeof(int)),
        new SchemaColumn(nameof(ProfilingOverheadEntity.Score), 5, typeof(int))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(ProfilingOverheadEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns
            .Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
