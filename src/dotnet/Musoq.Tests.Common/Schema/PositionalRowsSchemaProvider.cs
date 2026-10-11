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
    IReadOnlyList<object[]> rows) : SchemaBase("positional", CreateLibrary()), IQueryScopedRowSourceSchema
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

    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows | SourceTransferCapabilities.LogicalScalarReads
        };

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(string name,
        QueryScopedRowSourceRequest request, TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, global::Musoq.Schema.ISourceArguments
    {
        if (!name.Equals(All, StringComparison.OrdinalIgnoreCase)) throw new NotSupportedException(name);
        return new PositionalRowsSource<TRow, TMaterializer>(rows, request.Shape);
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext,
        TArguments parameters)
        => throw new InvalidOperationException("This test source requires native query-scoped transfer.");

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

internal sealed class PositionalRowsSource<TRow, TMaterializer>(IReadOnlyList<object[]> rows, QueryRowShape shape)
    : RowSourceBase<TRow> where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var materialized = new List<TRow>(rows.Count);
        foreach (var row in rows)
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            var reader = new FixtureReader(row, shape);
            materialized.Add(TMaterializer.Materialize(ref reader));
        }
        writer.Write(materialized);
    }

    private readonly struct FixtureReader(object[] values, QueryRowShape shape) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot) => (T)values[shape.Fields[slot].SourceColumnIndex];
    }
}
