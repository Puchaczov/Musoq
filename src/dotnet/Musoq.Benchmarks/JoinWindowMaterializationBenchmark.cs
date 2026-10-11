using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class JoinWindowMaterializationBenchmark
{
    private const string Query =
        "select a.Name, b.City, Sum(a.Population) over (order by a.Name rows between 1 preceding and current row) as RunSum " +
        "from #test.entities() a inner join #test.entities() b on a.Id = b.Id";

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _query = null!;
    private int _expectedRowCount;
    private decimal _expectedChecksum;

    public enum InputOrder
    {
        Ascending,
        Shuffled
    }

    [Params(1000, 10000, 50000)]
    public int RowsCount { get; set; }

    [Params(1, 2)]
    public int DuplicateFactor { get; set; }

    [Params(InputOrder.Ascending, InputOrder.Shuffled)]
    public InputOrder Order { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var entities = Enumerable.Range(0, RowsCount)
            .Select(index => new JoinWindowEntity
            {
                Id = index / DuplicateFactor,
                Name = $"Name-{GetOrderIndex(index, RowsCount, Order):D8}",
                Population = 1 + index % 17,
                City = $"City-{index:D8}"
            })
            .ToArray();

        _expectedRowCount = checked(RowsCount * DuplicateFactor);
        _expectedChecksum = ComputeExpectedChecksum(entities, DuplicateFactor);
        _query = InstanceCreator.CompileForExecution(
            Query,
            Guid.NewGuid().ToString(),
            new JoinWindowSchemaProvider(entities),
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized());

        var actualChecksum = ExecuteAndConsume();
        if (actualChecksum != _expectedChecksum)
        {
            _query.Dispose();
            throw new InvalidOperationException(
                $"The join/window benchmark produced checksum {actualChecksum}, expected {_expectedChecksum}.");
        }
    }

    [Benchmark]
    public decimal ExecuteAndConsume()
    {
        using var result = _query.Run();
        var rowCount = 0;
        var checksum = 0m;
        foreach (var row in result)
        {
            if (row.Count != 3)
                throw new InvalidOperationException($"Expected 3 output columns, received {row.Count}.");

            checksum += row.GetValue<decimal?>(2).GetValueOrDefault();
            rowCount++;
        }

        if (rowCount != _expectedRowCount)
        {
            throw new InvalidOperationException(
                $"The join/window benchmark produced {rowCount} rows, expected {_expectedRowCount}.");
        }

        if (checksum != _expectedChecksum)
        {
            throw new InvalidOperationException(
                $"The join/window benchmark produced checksum {checksum}, expected {_expectedChecksum}.");
        }

        return checksum;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _query.Dispose();
    }

    private static int GetOrderIndex(int index, int count, InputOrder order) => order switch
    {
        InputOrder.Ascending => index,
        InputOrder.Shuffled => (index * 7919) % count,
        _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
    };

    private static decimal ComputeExpectedChecksum(
        IReadOnlyCollection<JoinWindowEntity> entities,
        int duplicateFactor)
    {
        var checksum = 0m;
        var hasPrevious = false;
        var previousPopulation = 0m;
        foreach (var entity in entities.OrderBy(static entity => entity.Name, StringComparer.Ordinal))
        {
            checksum += entity.Population;
            if (hasPrevious)
                checksum += previousPopulation;

            for (var duplicate = 1; duplicate < duplicateFactor; duplicate++)
                checksum += entity.Population * 2;

            previousPopulation = entity.Population;
            hasPrevious = true;
        }

        return checksum;
    }

    public sealed class JoinWindowEntity
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public decimal Population { get; init; }

        public string City { get; init; } = string.Empty;
    }

    private sealed class JoinWindowSchemaProvider(IReadOnlyList<JoinWindowEntity> entities) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new JoinWindowSchema(entities);
    }

    private sealed class JoinWindowSchema(IReadOnlyList<JoinWindowEntity> entities)
        : SchemaBase("test", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => new JoinWindowTable();

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext executionContext,
            TArguments parameters) => EnsureSourceType<T, JoinWindowEntity>(
            name,
            new EntitySource<JoinWindowEntity>(
                BenchmarkSourceChunks.Create(entities),
                new Dictionary<string, int>
                {
                    { nameof(JoinWindowEntity.Name), 0 },
                    { nameof(JoinWindowEntity.Population), 1 },
                    { nameof(JoinWindowEntity.Id), 2 },
                    { nameof(JoinWindowEntity.City), 3 }
                },
                new Dictionary<int, Func<JoinWindowEntity, object?>>
                {
                    { 0, entity => entity.Name },
                    { 1, entity => entity.Population },
                    { 2, entity => entity.Id },
                    { 3, entity => entity.City }
                }));

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodManager);
        }
    }

    private sealed class JoinWindowTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(JoinWindowEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(JoinWindowEntity.Population), 1, typeof(decimal)),
            new SchemaColumn(nameof(JoinWindowEntity.Id), 2, typeof(int)),
            new SchemaColumn(nameof(JoinWindowEntity.City), 3, typeof(string))
        ];

        public ISchemaColumn GetColumnByName(string name) => Columns.First(column => column.ColumnName == name);

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName == name).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(JoinWindowEntity));
    }

    private sealed class Library : LibraryBase;
}
