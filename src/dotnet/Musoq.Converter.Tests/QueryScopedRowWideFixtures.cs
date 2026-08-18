using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public sealed class WideQueryRowsSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema) => new WideQueryRowsSchema();
}

public sealed class WideQueryRowsSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    public WideQueryRowsSchema()
        : base("widequeryrows", CreateLibrary())
    {
        AddTable<WideQueryRowsTable>("items");
        AddSource<WideQueryRowsLegacySource>("items");
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        return base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows
        };
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        return new WideQueryRowsMaterializedSource<TRow, TMaterializer>(request.Shape.Fields);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new EmptyLibrary());
        return new MethodsAggregator(methods);
    }
}

public sealed class WideQueryRowsTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = Enumerable.Range(0, 5)
        .Select(index => (ISchemaColumn)new SchemaColumn($"G{index}", index, typeof(Guid)))
        .ToArray();

    public SchemaTableMetadata Metadata { get; } = new(typeof(WideQueryRowsEntity));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal)).ToArray();
}

public sealed class WideQueryRowsLegacySource : RowSourceBase<WideQueryRowsEntity>
{
    protected override void CollectChunks(IChunkWriter<WideQueryRowsEntity> writer)
    {
        writer.Write([new WideQueryRowsEntity()]);
    }
}

public sealed class WideQueryRowsMaterializedSource<TRow, TMaterializer> : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    private readonly IReadOnlyList<QueryRowField> _fields;

    public WideQueryRowsMaterializedSource(IReadOnlyList<QueryRowField> fields)
    {
        _fields = fields;
    }

    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var reader = new WideQueryRowsReader(_fields);
        writer.Write([TMaterializer.Materialize<WideQueryRowsReader>(ref reader)]);
    }
}

public ref struct WideQueryRowsReader(IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    private static readonly Guid Value = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public T Read<T>(int slot)
    {
        if (fields[slot].SourceColumnIndex is < 0 or > 4)
            throw new ArgumentOutOfRangeException(nameof(slot));

        return (T)(object)Value;
    }
}

public sealed class WideQueryRowsEntity
{
    public Guid G0 { get; init; }

    public Guid G1 { get; init; }

    public Guid G2 { get; init; }

    public Guid G3 { get; init; }

    public Guid G4 { get; init; }
}
