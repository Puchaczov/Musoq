using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests.Schema;

public sealed class DynamicRowsSchemaProvider(
    IReadOnlyDictionary<string, Type> columns,
    IReadOnlyList<IReadOnlyDictionary<string, object>> rows,
    IReadOnlyList<ISchemaColumn>? schemaColumns = null)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        var resolvedColumns = schemaColumns ?? columns
            .Select((column, index) => (ISchemaColumn)new SchemaColumn(column.Key, index, column.Value))
            .ToArray();
        return new DynamicRowsSchema(resolvedColumns, rows);
    }
}

public sealed class DynamicRowsSchema(
    IReadOnlyList<ISchemaColumn> columns,
    IReadOnlyList<IReadOnlyDictionary<string, object>> rows)
    : SchemaBase(Dynamic, CreateLibrary()), IQueryScopedRowSourceSchema
{
    private const string All = "all";
    private const string Dynamic = "dynamic";

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        if (string.Equals(name, All, StringComparison.OrdinalIgnoreCase))
            return new DynamicRowsTable(columns);

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
    {
        if (string.Equals(name, All, StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, IReadOnlyDictionary<string, object>>(name, new DynamicRowsSource(rows));

        throw new NotSupportedException(name);
    }

    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with { TransferCapabilities = SourceTransferCapabilities.QueryScopedRows };

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
        string name, QueryScopedRowSourceRequest request, TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, ISourceArguments
        => new NativeDictionaryTestSource<TRow, TMaterializer>(
            rows.Select(row => (IReadOnlyDictionary<string, object?>)row.ToDictionary(pair => pair.Key, pair => (object?)pair.Value)).ToArray(),
            request.Shape.Fields);

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new EmptyLibrary());

        return new MethodsAggregator(methodsManager);
    }
}

public sealed class DynamicRowsTable : ISchemaTable
{
    public DynamicRowsTable(IReadOnlyList<ISchemaColumn> columns)
    {
        Columns = columns.ToArray();
    }

    public ISchemaColumn[] Columns { get; }

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(QueryScopedRowContract));
}

public sealed class DynamicRowsSource(IReadOnlyList<IReadOnlyDictionary<string, object>> rows)
    : RowSourceBase<IReadOnlyDictionary<string, object>>
{
    protected override void CollectChunks(IChunkWriter<IReadOnlyDictionary<string, object>> writer)
    {
        writer.Write(rows.ToArray());
    }
}
