using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Converter.Tests.Schema;

public sealed class EntitySetSchemaProvider(
    IReadOnlyDictionary<string, IReadOnlyList<EntitySetEntity>> rowsBySchema)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        var key = schema.StartsWith('#') ? schema : $"#{schema}";
        if (!rowsBySchema.TryGetValue(key, out var rows))
            throw new NotSupportedException(schema);

        return new EntitySetSchema(key, rows);
    }
}

public sealed class EntitySetSchema(
    string schemaName,
    IReadOnlyList<EntitySetEntity> rows)
    : SchemaBase(schemaName, CreateLibrary())
{
    private const string Entities = "entities";

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        if (string.Equals(name, Entities, StringComparison.OrdinalIgnoreCase))
            return new EntitySetTable();

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        if (string.Equals(name, Entities, StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, EntitySetEntity>(name, new EntitySetRowSource(rows));

        throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new EmptyLibrary());

        return new MethodsAggregator(methodsManager);
    }
}

public sealed class EntitySetTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(EntitySetEntity.City), 0, typeof(string)),
        new SchemaColumn(nameof(EntitySetEntity.Country), 1, typeof(string)),
        new SchemaColumn(nameof(EntitySetEntity.Population), 2, typeof(decimal)),
        new SchemaColumn(nameof(EntitySetEntity.Name), 3, typeof(string))
    ];

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(EntitySetEntity));
}

public sealed class EntitySetRowSource(IReadOnlyList<EntitySetEntity> rows)
    : RowSourceBase<EntitySetEntity>
{
    protected override void CollectChunks(IChunkWriter<EntitySetEntity> writer)
    {
        writer.Write(rows.ToArray());
    }
}

public sealed class EntitySetEntity
{
    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public decimal Population { get; init; }

    public string Name { get; init; } = string.Empty;
}
