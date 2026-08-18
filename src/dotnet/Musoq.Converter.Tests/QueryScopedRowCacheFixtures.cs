using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Converter.Tests.Schema;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

public sealed record QueryRowCacheColumn(string Name, int Ordinal, Type Type);

public sealed class QueryRowCacheState
{
    public QueryRowCacheState(
        IReadOnlyList<QueryRowCacheColumn> columns,
        IReadOnlyList<object?[]> rows,
        SourceTransferCapabilities transferCapabilities = SourceTransferCapabilities.QueryScopedRows)
    {
        Configure(columns, rows, transferCapabilities);
    }

    public IReadOnlyList<QueryRowCacheColumn> Columns { get; private set; } = [];

    public IReadOnlyList<object?[]> Rows { get; private set; } = [];

    public SourceTransferCapabilities TransferCapabilities { get; private set; }

    public void Configure(
        IReadOnlyList<QueryRowCacheColumn> columns,
        IReadOnlyList<object?[]> rows,
        SourceTransferCapabilities transferCapabilities = SourceTransferCapabilities.QueryScopedRows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);
        Columns = columns.ToArray();
        Rows = rows.Select(static row => (object?[])row.Clone()).ToArray();
        TransferCapabilities = transferCapabilities;
    }
}

public sealed class QueryRowCacheSchemaProvider(QueryRowCacheState state) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (string.Equals(schema, "queryrowcache", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, "#queryrowcache", StringComparison.OrdinalIgnoreCase))
        {
            return new QueryRowCacheSchema(state);
        }

        throw new NotSupportedException(schema);
    }
}

public sealed class QueryRowCacheSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    private readonly QueryRowCacheState _state;

    public QueryRowCacheSchema(QueryRowCacheState state)
        : base("queryrowcache", CreateLibrary())
    {
        _state = state;
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
            return new QueryRowCacheTable(_state);

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSourceType<T, QueryRowCacheEntity>(
                name,
                new QueryRowCacheLegacySource(_state));
        }

        throw new NotSupportedException(name);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        return base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = _state.TransferCapabilities
        };
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        if (!string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(name);

        return new QueryRowCacheMaterializedSource<TRow, TMaterializer>(
            _state.Rows,
            request.Shape.Fields);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new EmptyLibrary());
        return new MethodsAggregator(methods);
    }
}

public sealed class QueryRowCacheTable(QueryRowCacheState state) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = state.Columns
        .Select(static column => (ISchemaColumn)new SchemaColumn(column.Name, column.Ordinal, column.Type))
        .ToArray();

    public SchemaTableMetadata Metadata { get; } = new(typeof(QueryRowCacheEntity));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal)).ToArray();
}

public sealed class QueryRowCacheLegacySource(QueryRowCacheState state) : RowSourceBase<QueryRowCacheEntity>
{
    protected override void CollectChunks(IChunkWriter<QueryRowCacheEntity> writer)
    {
        writer.Write(state.Rows.Select(row => QueryRowCacheEntity.Create(state.Columns, row)).ToArray());
    }
}

public sealed class QueryRowCacheMaterializedSource<TRow, TMaterializer>(
    IReadOnlyList<object?[]> rows,
    IReadOnlyList<QueryRowField> fields) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var materialized = new List<TRow>(rows.Count);
        foreach (var row in rows)
        {
            var reader = new QueryRowCacheReader(row, fields);
            materialized.Add(TMaterializer.Materialize<QueryRowCacheReader>(ref reader));
        }

        writer.Write(materialized);
    }
}

public ref struct QueryRowCacheReader(
    IReadOnlyList<object?> values,
    IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var value = values[fields[slot].SourceColumnIndex];
        return value is null ? default! : (T)value;
    }
}

public sealed class QueryRowCacheEntity
{
    public int Value { get; set; }

    public string Text { get; set; } = string.Empty;

    public Guid G0 { get; set; }

    public Guid G1 { get; set; }

    public Guid G2 { get; set; }

    public Guid G3 { get; set; }

    public Guid G4 { get; set; }

    public static QueryRowCacheEntity Create(
        IReadOnlyList<QueryRowCacheColumn> columns,
        IReadOnlyList<object?> values)
    {
        var entity = new QueryRowCacheEntity();
        foreach (var column in columns)
        {
            var value = values[column.Ordinal];
            switch (column.Name)
            {
                case nameof(Value):
                    entity.Value = (int)value!;
                    break;
                case nameof(Text):
                    entity.Text = (string)value!;
                    break;
                case nameof(G0):
                    entity.G0 = (Guid)value!;
                    break;
                case nameof(G1):
                    entity.G1 = (Guid)value!;
                    break;
                case nameof(G2):
                    entity.G2 = (Guid)value!;
                    break;
                case nameof(G3):
                    entity.G3 = (Guid)value!;
                    break;
                case nameof(G4):
                    entity.G4 = (Guid)value!;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported cache-fixture column '{column.Name}'.");
            }
        }

        return entity;
    }
}
