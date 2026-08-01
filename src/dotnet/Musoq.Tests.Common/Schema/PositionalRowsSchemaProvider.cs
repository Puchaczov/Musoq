using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;

namespace Musoq.Tests.Common.Schema;

public sealed class PositionalRowsSchemaProvider(
    IReadOnlyList<ISchemaColumn> columns,
    IReadOnlyList<object[]> rows) : ISchemaProvider
{
    public ISchema GetSchema(string schema) => new PositionalRowsSchema(columns, rows);
}

public sealed class PositionalRowsSchema(
    IReadOnlyList<ISchemaColumn> columns,
    IReadOnlyList<object[]> rows) : SchemaBase("positional", CreateLibrary())
{
    private const string All = "all";

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (name.Equals(All, StringComparison.OrdinalIgnoreCase))
            return new PositionalRowsTable(columns);

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (name.Equals(All, StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, object[]>(name, new PositionalRowsSource(rows));

        throw new NotSupportedException(name);
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        return methodName.Equals(All, StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(All, ConstructorInfo.Empty())]
            : [];
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class PositionalRowsTable(IReadOnlyList<ISchemaColumn> columns) : ISchemaTable
{
    public ISchemaColumn[] Columns => columns.ToArray();

    public ISchemaColumn? GetColumnByName(string name) =>
        columns.SingleOrDefault(column =>
            column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        columns.Where(column =>
            column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

    public SchemaTableMetadata Metadata { get; } = new(typeof(object[]));
}

public sealed class PositionalRowsSource(IReadOnlyList<object[]> rows) : RowSourceBase<object[]>
{
    protected override void CollectChunks(IChunkWriter<object[]> writer)
    {
        writer.Write(rows);
    }
}
