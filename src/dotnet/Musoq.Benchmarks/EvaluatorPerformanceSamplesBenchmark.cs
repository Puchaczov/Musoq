using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Tests.Common;

namespace Musoq.Benchmarks;

public enum EvaluatorPerformanceScenario
{
    Q227_PerformanceJoinAggregate,
    Q228_PerformanceWideCorrelatedSubquery,
    Q229_PerformanceWindowCteSetOperation,
    Q230_PerformanceTableProjection
}

[MemoryDiagnoser]
public class EvaluatorPerformanceSamplesBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;

    [Params(1_000, 10_000)]
    public int RowsCount { get; set; }

    [Params(
        EvaluatorPerformanceScenario.Q227_PerformanceJoinAggregate,
        EvaluatorPerformanceScenario.Q228_PerformanceWideCorrelatedSubquery,
        EvaluatorPerformanceScenario.Q229_PerformanceWindowCteSetOperation,
        EvaluatorPerformanceScenario.Q230_PerformanceTableProjection)]
    public EvaluatorPerformanceScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _query = EvaluatorPerformanceBenchmarkSupport.Compile(
            Scenario,
            RowsCount,
            _loggerResolver);
    }

    [Benchmark]
    public int RunQuery()
    {
        return EvaluatorPerformanceBenchmarkSupport.Materialize(_query);
    }
}

public static class EvaluatorPerformanceBenchmarkSupport
{
    public static CompiledQuery Compile(
        EvaluatorPerformanceScenario scenario,
        int rowsCount,
        ILoggerResolver? loggerResolver = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowsCount);

        var provider = scenario == EvaluatorPerformanceScenario.Q227_PerformanceJoinAggregate
            ? CreateQ227Provider(rowsCount)
            : CreateTypedProvider(scenario, rowsCount);
        var options = BenchmarkCompilationOptions.Materialized();

        return InstanceCreator.CompileForExecution(
            QueryFor(scenario),
            Guid.NewGuid().ToString(),
            provider,
            loggerResolver ?? new BenchmarkLoggerResolver(),
            options);
    }

    public static int Materialize(CompiledQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.Run().Count;
    }

    private static string QueryFor(EvaluatorPerformanceScenario scenario)
    {
        return scenario switch
        {
            EvaluatorPerformanceScenario.Q227_PerformanceJoinAggregate => EvaluatorPerformanceQueries.Q227,
            EvaluatorPerformanceScenario.Q228_PerformanceWideCorrelatedSubquery => EvaluatorPerformanceQueries.Q228,
            EvaluatorPerformanceScenario.Q229_PerformanceWindowCteSetOperation => EvaluatorPerformanceQueries.Q229,
            EvaluatorPerformanceScenario.Q230_PerformanceTableProjection => EvaluatorPerformanceQueries.Q230,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    private static ISchemaProvider CreateTypedProvider(
        EvaluatorPerformanceScenario scenario,
        int rowsCount)
    {
        var left = CreateTypedRows(rowsCount);
        var right = scenario == EvaluatorPerformanceScenario.Q228_PerformanceWideCorrelatedSubquery
            ? CreateTypedRows((rowsCount + 1) / 2, index => index * 2)
            : scenario == EvaluatorPerformanceScenario.Q229_PerformanceWindowCteSetOperation
                ? left
                : [];

        return new EvaluatorPerformanceSchemaProvider(
            new Dictionary<string, IReadOnlyList<EvaluatorPerformanceEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = left,
                ["#A"] = left,
                ["B"] = right,
                ["#B"] = right
            });
    }

    private static ISchemaProvider CreateQ227Provider(int rowsCount)
    {
        var left = CreateQ227Rows(rowsCount, "left");
        var right = CreateQ227Rows(rowsCount, "right");
        return new Q227BaselineSchemaProvider(
            new Dictionary<string, IReadOnlyList<PerformanceJoinBenchmarkEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = left,
                ["#A"] = left,
                ["B"] = right,
                ["#B"] = right
            });
    }

    private static IReadOnlyList<EvaluatorPerformanceEntity> CreateTypedRows(
        int count,
        Func<int, int>? sourceIndex = null)
    {
        sourceIndex ??= static index => index;
        return Enumerable.Range(0, count)
            .Select(index => CreateTypedEntity(sourceIndex(index)))
            .ToArray();
    }

    private static EvaluatorPerformanceEntity CreateTypedEntity(int index)
    {
        return new EvaluatorPerformanceEntity
        {
            Id = index,
            Name = $"Name_{index}",
            City = $"City_{index % 64}",
            Country = $"Country_{index % 2}",
            Population = index * 10m,
            Month = $"Month_{index % 12}",
            Money = index * 1.25m,
            NullableValue = index % 5 == 0 ? null : index
        };
    }

    private static IReadOnlyList<PerformanceJoinBenchmarkEntity> CreateQ227Rows(int count, string prefix)
    {
        return Enumerable.Range(0, count)
            .Select(index => new PerformanceJoinBenchmarkEntity
            {
                Id = index,
                Name = $"{prefix}_{index}",
                City = $"City_{index % 64}",
                Country = $"Country_{index % 8}",
                Population = index * 10
            })
            .ToArray();
    }

    private sealed class Q227BaselineSchemaProvider(
        IReadOnlyDictionary<string, IReadOnlyList<PerformanceJoinBenchmarkEntity>> rowsBySchema) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!rowsBySchema.TryGetValue(schema, out var rows))
                throw new NotSupportedException(schema);

            return new Q227BaselineSchema(rows);
        }
    }

    private sealed class Q227BaselineSchema(IReadOnlyList<PerformanceJoinBenchmarkEntity> rows)
        : SchemaBase("Q227Baseline", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return new Q227BaselineTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, PerformanceJoinBenchmarkEntity>(
                    name,
                    new Q227BaselineRowSource(rows));

            throw new NotSupportedException(name);
        }
    }

    private sealed class Q227BaselineTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(PerformanceJoinBenchmarkEntity.Id), 0, typeof(int)),
            new SchemaColumn(nameof(PerformanceJoinBenchmarkEntity.Name), 1, typeof(string)),
            new SchemaColumn(nameof(PerformanceJoinBenchmarkEntity.City), 2, typeof(string)),
            new SchemaColumn(nameof(PerformanceJoinBenchmarkEntity.Country), 3, typeof(string)),
            new SchemaColumn(nameof(PerformanceJoinBenchmarkEntity.Population), 4, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(PerformanceJoinBenchmarkEntity));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName == name);

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName == name).ToArray();
    }

    private sealed class Q227BaselineRowSource(IReadOnlyList<PerformanceJoinBenchmarkEntity> rows)
        : RowSourceBase<PerformanceJoinBenchmarkEntity>
    {
        protected override void CollectChunks(IChunkWriter<PerformanceJoinBenchmarkEntity> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new EvaluatorPerformanceLibrary());
        return new MethodsAggregator(methodsManager);
    }

    private sealed class EvaluatorPerformanceLibrary : LibraryBase;
}

public sealed class EvaluatorPerformanceEntity
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public decimal Population { get; init; }

    public string Month { get; init; } = string.Empty;

    public decimal Money { get; init; }

    public int? NullableValue { get; init; }
}

public sealed class PerformanceJoinBenchmarkEntity
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public int Population { get; init; }
}

public sealed class EvaluatorPerformanceSchemaProvider(
    IReadOnlyDictionary<string, IReadOnlyList<EvaluatorPerformanceEntity>> rowsBySchema)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!rowsBySchema.TryGetValue(schema, out var rows))
            throw new NotSupportedException(schema);

        return new EvaluatorPerformanceSchema(rows);
    }
}

internal sealed class EvaluatorPerformanceSchema(
    IReadOnlyList<EvaluatorPerformanceEntity> rows)
    : SchemaBase("EvaluatorPerformance", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
            return new EvaluatorPerformanceTable();

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, EvaluatorPerformanceEntity>(
                name,
                new EvaluatorPerformanceRowSource(rows));

        throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new EvaluatorPerformanceLibrary());
        return new MethodsAggregator(methodsManager);
    }

    private sealed class EvaluatorPerformanceLibrary : LibraryBase;
}

internal sealed class EvaluatorPerformanceTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.City), 2, typeof(string)),
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.Country), 3, typeof(string)),
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.Population), 4, typeof(decimal)),
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.Month), 5, typeof(string)),
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.Money), 6, typeof(decimal)),
        new SchemaColumn(nameof(EvaluatorPerformanceEntity.NullableValue), 7, typeof(int?))
    ];

    public SchemaTableMetadata Metadata { get; } =
        new(typeof(EvaluatorPerformanceEntity));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => column.ColumnName == name);

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => column.ColumnName == name).ToArray();
}

internal sealed class EvaluatorPerformanceRowSource(
    IReadOnlyList<EvaluatorPerformanceEntity> rows)
    : RowSourceBase<EvaluatorPerformanceEntity>
{
    protected override void CollectChunks(
        IChunkWriter<EvaluatorPerformanceEntity> writer)
    {
        writer.Write(rows.ToArray());
    }
}
