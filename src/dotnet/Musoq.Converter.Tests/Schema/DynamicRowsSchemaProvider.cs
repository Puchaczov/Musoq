using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Converter.Tests.Schema;

public sealed class DynamicRowsSchemaProvider(
    IReadOnlyDictionary<string, Type> columns,
    IReadOnlyList<IReadOnlyDictionary<string, object>> rows)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new DynamicRowsSchema(columns, rows);
    }
}

public sealed class DynamicRowsSchema(
    IReadOnlyDictionary<string, Type> columns,
    IReadOnlyList<IReadOnlyDictionary<string, object>> rows)
    : SchemaBase(Dynamic, CreateLibrary())
{
    private const string All = "all";
    private const string Dynamic = "dynamic";

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        if (string.Equals(name, All, StringComparison.OrdinalIgnoreCase))
            return new DynamicRowsTable(columns);

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        if (string.Equals(name, All, StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, IReadOnlyDictionary<string, object>>(name, new DynamicRowsSource(rows));

        throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new EmptyLibrary());

        return new MethodsAggregator(methodsManager);
    }
}

public sealed class DynamicRowsTable : ISchemaTable
{
    public DynamicRowsTable(IReadOnlyDictionary<string, Type> columns)
    {
        Columns = columns
            .Select((column, index) => (ISchemaColumn)new SchemaColumn(column.Key, index, column.Value))
            .ToArray();
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

    public SchemaTableMetadata Metadata { get; } = new(typeof(IReadOnlyDictionary<string, object>));
}

public sealed class DynamicRowsSource(IReadOnlyList<IReadOnlyDictionary<string, object>> rows)
    : RowSourceBase<IReadOnlyDictionary<string, object>>
{
    protected override void CollectChunks(IChunkWriter<IReadOnlyDictionary<string, object>> writer)
    {
        writer.Write(rows.ToArray());
    }
}
