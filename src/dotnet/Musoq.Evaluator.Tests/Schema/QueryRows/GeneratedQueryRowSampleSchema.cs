using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests.Schema.QueryRows;

public enum GeneratedQueryRowSampleShape
{
    Narrow,
    Wide,
    SpecialNames,
    Enum
}

public sealed class GeneratedQueryRowSampleSchemaProvider(
    GeneratedQueryRowSampleShape shape,
    bool queryScopedRowsEnabled) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "queryrowsample", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new GeneratedQueryRowSampleSchema(shape, queryScopedRowsEnabled);
    }
}

public sealed class GeneratedQueryRowSampleSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    private readonly GeneratedQueryRowSampleDefinition _definition;
    private readonly bool _queryScopedRowsEnabled;
    private readonly GeneratedQueryRowSampleShape _shape;

    public GeneratedQueryRowSampleSchema(
        GeneratedQueryRowSampleShape shape,
        bool queryScopedRowsEnabled)
        : base("queryrowsample", CreateMethods())
    {
        _shape = shape;
        _definition = GeneratedQueryRowSampleDefinition.Create(shape);
        _queryScopedRowsEnabled = queryScopedRowsEnabled;
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
        {
            var columns = _shape == GeneratedQueryRowSampleShape.Enum &&
                          metadataContext.AllColumns.Count > 0
                ? metadataContext.AllColumns.ToArray()
                : _definition.Columns;
            return new GeneratedQueryRowSampleTable(columns);
        }

        return base.GetTableByName(name, metadataContext, parameters);
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        return string.Equals(methodName, "rows", StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(methodName, SchemaConstructorInfo.Empty())]
            : [];
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters);
        return descriptor with
        {
            Columns = _shape == GeneratedQueryRowSampleShape.Enum &&
                      context.MetadataContext.AllColumns.Count > 0
                ? context.MetadataContext.AllColumns.ToArray()
                : descriptor.Columns,
            TransferCapabilities = _queryScopedRowsEnabled
                ? SourceTransferCapabilities.QueryScopedRows |
                  (_shape == GeneratedQueryRowSampleShape.Enum
                      ? SourceTransferCapabilities.LogicalScalarReads
                      : SourceTransferCapabilities.None)
                : SourceTransferCapabilities.None
        };
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (!string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
            return base.GetRowSource<T>(name, executionContext, parameters);
        if (typeof(T) != typeof(object[]))
            throw new InvalidOperationException($"Legacy query-row sample requested unsupported row type '{typeof(T)}'.");

        return (RowSource<T>)(object)new GeneratedQueryRowSampleLegacySource(_definition.Rows);
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        if (!_queryScopedRowsEnabled)
            throw new InvalidOperationException("Query-scoped rows were not enabled for this sample.");
        if (!string.Equals(name, "rows", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(name);

        return new GeneratedQueryRowSampleSource<TRow, TMaterializer>(
            _definition.Rows,
            request.Shape.Fields);
    }

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }
}

public sealed class GeneratedQueryRowSampleTable(ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(typeof(object[]));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

public sealed class GeneratedQueryRowSampleLegacySource(IReadOnlyList<object?[]> rows)
    : RowSourceBase<object[]>
{
    protected override void CollectChunks(IChunkWriter<object[]> writer)
    {
        writer.Write(rows.Cast<object[]>().ToArray());
    }
}

public sealed class GeneratedQueryRowSampleSource<TRow, TMaterializer>(
    IReadOnlyList<object?[]> rows,
    IReadOnlyList<QueryRowField> fields) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var materialized = new List<TRow>(rows.Count);
        foreach (var row in rows)
        {
            var reader = new GeneratedQueryRowSampleReader(row, fields);
            materialized.Add(TMaterializer.Materialize<GeneratedQueryRowSampleReader>(ref reader));
        }

        writer.Write(materialized);
    }
}

public ref struct GeneratedQueryRowSampleReader(
    object?[] row,
    IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var value = row[fields[slot].SourceColumnIndex];
        return value is null ? default! : (T)value;
    }
}

internal sealed record GeneratedQueryRowSampleDefinition(
    ISchemaColumn[] Columns,
    IReadOnlyList<object?[]> Rows)
{
    public static GeneratedQueryRowSampleDefinition Create(GeneratedQueryRowSampleShape shape)
    {
        return shape switch
        {
            GeneratedQueryRowSampleShape.Narrow => new GeneratedQueryRowSampleDefinition(
                [
                    new SchemaColumn("Id", 0, typeof(int)),
                    new SchemaColumn("Name", 1, typeof(string))
                ],
                [
                    [1, "alpha"],
                    [2, "beta"]
                ]),
            GeneratedQueryRowSampleShape.Wide => new GeneratedQueryRowSampleDefinition(
                Enumerable.Range(0, 5)
                    .Select(index => (ISchemaColumn)new SchemaColumn($"G{index}", index, typeof(Guid)))
                    .ToArray(),
                [
                    Enumerable.Range(0, 5)
                        .Select(index => (object?)Guid.Parse($"00000000-0000-0000-0000-{index + 1:000000000000}"))
                        .ToArray()
                ]),
            GeneratedQueryRowSampleShape.SpecialNames => new GeneratedQueryRowSampleDefinition(
                [
                    new SchemaColumn("display name", 0, typeof(string)),
                    new SchemaColumn("na-me", 1, typeof(int)),
                    new SchemaColumn("MiastoŁódź", 2, typeof(string)),
                    new SchemaColumn("select", 3, typeof(string))
                ],
                [
                    ["visible", 7, "Łódź", "keyword"]
                ]),
            GeneratedQueryRowSampleShape.Enum => new GeneratedQueryRowSampleDefinition(
                [
                    new SchemaColumn("Id", 0, typeof(int)),
                    new SchemaColumn("Status", 1, typeof(short)),
                    new SchemaColumn("Access", 2, typeof(uint))
                ],
                [
                    [1, (short)20, 3u]
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
        };
    }
}
