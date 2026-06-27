using System.Threading;
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

[MemoryDiagnoser]
public class JoinAggregateProjectionBenchmark
{
    public enum JoinAggregateScenario
    {
        AggregateOverHashJoin,
        CteBackedAggregateOverHashJoin
    }

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1_000, 10_000)]
    public int RowsCount { get; set; }

    [Params(
        JoinAggregateScenario.AggregateOverHashJoin,
        JoinAggregateScenario.CteBackedAggregateOverHashJoin)]
    public JoinAggregateScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var script = Scenario switch
        {
            JoinAggregateScenario.AggregateOverHashJoin =>
                "select a.City as City, Count(b.Name) as MatchCount from #A.entities() a inner join #B.entities() b on a.City = b.City group by a.City",

            JoinAggregateScenario.CteBackedAggregateOverHashJoin =>
                "with leftCte as (select a.City as City from #A.entities() a), rightCte as (select b.City as City, b.Name as Name from #B.entities() b) select l.City as City, Count(r.Name) as MatchCount from leftCte l inner join rightCte r on l.City = r.City group by l.City",

            _ => throw new ArgumentOutOfRangeException()
        };

        var leftRows = CreateRows(RowsCount, "left", 64);
        var rightRows = CreateRows(RowsCount, "right", 64);
        var schemaProvider = new JoinAggregateSchemaProvider(
            new Dictionary<string, IReadOnlyList<JoinAggregateEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = leftRows,
                ["#A"] = leftRows,
                ["B"] = rightRows,
                ["#B"] = rightRows
            });

        _query = InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized());
    }

    [Benchmark]
    public Table RunQuery()
    {
        return _query.Run();
    }

    private static JoinAggregateEntity[] CreateRows(int count, string prefix, int cityCardinality)
    {
        return Enumerable.Range(0, count)
            .Select(index => new JoinAggregateEntity
            {
                Id = index,
                Name = $"{prefix}_{index}",
                City = $"City_{index % cityCardinality}",
                Country = $"Country_{index % 8}",
                Population = index * 10
            })
            .ToArray();
    }

    private sealed class JoinAggregateEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
            new Dictionary<string, int>
            {
                [nameof(Id)] = 0,
                [nameof(Name)] = 1,
                [nameof(City)] = 2,
                [nameof(Country)] = 3,
                [nameof(Population)] = 4
            };

        public static readonly IReadOnlyDictionary<int, Func<JoinAggregateEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<JoinAggregateEntity, object?>>
            {
                [0] = entity => entity.Id,
                [1] = entity => entity.Name,
                [2] = entity => entity.City,
                [3] = entity => entity.Country,
                [4] = entity => entity.Population
            };

        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public int Population { get; init; }
    }

    private sealed class JoinAggregateSchemaProvider(
        IReadOnlyDictionary<string, IReadOnlyList<JoinAggregateEntity>> rowsBySchema) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!rowsBySchema.TryGetValue(schema, out var rows))
                throw new NotSupportedException(schema);

            return new JoinAggregateSchema(schema.TrimStart('#'), rows);
        }
    }

    private sealed class JoinAggregateSchema(string name, IReadOnlyList<JoinAggregateEntity> rows)
        : SchemaBase(name, CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return new JoinAggregateTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, JoinAggregateEntity>(name, new JoinAggregateRowSource(rows));

            throw new NotSupportedException(name);
        }
    }

    private sealed class JoinAggregateTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(JoinAggregateEntity.Id), 0, typeof(int)),
            new SchemaColumn(nameof(JoinAggregateEntity.Name), 1, typeof(string)),
            new SchemaColumn(nameof(JoinAggregateEntity.City), 2, typeof(string)),
            new SchemaColumn(nameof(JoinAggregateEntity.Country), 3, typeof(string)),
            new SchemaColumn(nameof(JoinAggregateEntity.Population), 4, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(JoinAggregateEntity));

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class JoinAggregateRowSource(IReadOnlyList<JoinAggregateEntity> rows)
        : RowSourceBase<JoinAggregateEntity>
    {
        protected override void CollectChunks(IChunkWriter<JoinAggregateEntity> writer)
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
