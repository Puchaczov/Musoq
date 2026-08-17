using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class BenchmarkParitySchemaProvider(IReadOnlyList<BenchmarkParityEntity> rows) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new BenchmarkParitySchema(rows);
    }
}

public sealed class BenchmarkParitySchema(IReadOnlyList<BenchmarkParityEntity> rows)
    : SchemaBase("test", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return new BenchmarkParityTable();
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        return EnsureSourceType<T, BenchmarkParityEntity>(name, new BenchmarkParityRowSource(rows));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        methodsManager.RegisterLibraries(new BenchmarkParityLibrary());
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class BenchmarkParityTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(BenchmarkParityEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(BenchmarkParityEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(BenchmarkParityEntity.Value), 2, typeof(int)),
        new SchemaColumn(nameof(BenchmarkParityEntity.Category), 3, typeof(string)),
        new SchemaColumn(nameof(BenchmarkParityEntity.City), 4, typeof(string)),
        new SchemaColumn(nameof(BenchmarkParityEntity.Country), 5, typeof(string)),
        new SchemaColumn(nameof(BenchmarkParityEntity.Population), 6, typeof(decimal))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(BenchmarkParityEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class BenchmarkParityRowSource(IReadOnlyList<BenchmarkParityEntity> rows)
    : RowSourceBase<BenchmarkParityEntity>
{
    protected override void CollectChunks(IChunkWriter<BenchmarkParityEntity> writer)
    {
        writer.Write(rows.ToArray());
    }
}

public sealed class BenchmarkParityEntity
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Value { get; init; }

    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public decimal Population { get; init; }
}

public sealed class BenchmarkParityLibrary : LibraryBase
{
    [BindableMethod]
    public int ExpensiveMethod(int value)
    {
        return value * 2;
    }

    [BindableMethod]
    public decimal ExpensiveCompute(int value)
    {
        return value * 1.1m;
    }

    [BindableMethod]
    public string? StringTransform(string? value)
    {
        return value?.ToUpperInvariant();
    }

    [BindableMethod]
    public int HeavyComputation(int value)
    {
        return value * 5;
    }
}
