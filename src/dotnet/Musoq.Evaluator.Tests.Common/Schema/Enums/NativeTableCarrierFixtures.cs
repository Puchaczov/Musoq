using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.QueryRows;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public sealed class NativeTableCarrierSchemaProvider(Action? beforeRows = null, Action? sourceOpened = null) : Musoq.Schema.ISchemaProvider
{
    public Musoq.Schema.ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "specstressnative", System.StringComparison.OrdinalIgnoreCase))
            throw new System.NotSupportedException(schema);

        return new NativeTableCarrierSchema(beforeRows, sourceOpened);
    }
}

public sealed class NativeTableCarrierSchema :
    Musoq.Schema.DataSources.SchemaBase,
    Musoq.Schema.IQueryScopedRowSourceSchema
{
    private readonly Action? _beforeRows;
    private readonly Action? _sourceOpened;

    public NativeTableCarrierSchema(Action? beforeRows = null, Action? sourceOpened = null)
        : base("specstressnative", new Musoq.Schema.Managers.MethodsAggregator(new Musoq.Schema.Managers.MethodsManager()))
    {
        _beforeRows = beforeRows;
        _sourceOpened = sourceOpened;
    }

    public override Musoq.Schema.ISchemaTable GetTableByName(
        string name,
        Musoq.Schema.Optimization.SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (!string.Equals(name, "rows", System.StringComparison.OrdinalIgnoreCase))
            throw new System.NotSupportedException(name);

        var columns = metadataContext.AllColumns.Count > 0
            ? System.Linq.Enumerable.ToArray(metadataContext.AllColumns)
            : [new Musoq.Schema.DataSources.SchemaColumn("Status", 0, typeof(short))];
        return new NativeTableCarrierTable(columns);
    }

    public override Musoq.Schema.Reflection.SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        Musoq.Schema.Optimization.SourceMetadataContext metadataContext)
    {
        return string.Equals(methodName, "rows", System.StringComparison.OrdinalIgnoreCase)
            ? [new Musoq.Schema.Reflection.SchemaMethodInfo(
                methodName,
                Musoq.Schema.Reflection.ConstructorInfo.Empty())]
            : [];
    }

    public override Musoq.Schema.Optimization.SourceDescriptor DescribeSource(
        string name,
        Musoq.Schema.Optimization.SourceDescribeContext context,
        params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters);
        return descriptor with
        {
            Columns = context.MetadataContext.AllColumns.Count > 0
                ? System.Linq.Enumerable.ToArray(context.MetadataContext.AllColumns)
                : descriptor.Columns,
            TransferCapabilities = Musoq.Schema.SourceTransferCapabilities.QueryScopedRows |
                                  Musoq.Schema.SourceTransferCapabilities.LogicalScalarReads
        };
    }

    public override Musoq.Schema.DataSources.RowSource<T> GetRowSource<T, TArguments>(
        string name,
        Musoq.Schema.Optimization.SourceExecutionContext executionContext,
        TArguments nativeArguments)
    {
        if (!string.Equals(name, "rows", System.StringComparison.OrdinalIgnoreCase) || typeof(T) != typeof(object[]))
            throw new System.NotSupportedException(name);

        _sourceOpened?.Invoke();
        return (Musoq.Schema.DataSources.RowSource<T>)(object)new NativeTableCarrierLegacySource(_beforeRows);
    }

    public Musoq.Schema.DataSources.RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
        string name,
        Musoq.Schema.QueryScopedRowSourceRequest request,
        TArguments parameters)
        where TMaterializer : struct, Musoq.Schema.IQueryRowMaterializer<TRow>
        where TArguments : struct, Musoq.Schema.ISourceArguments
    {
        if (!string.Equals(name, "rows", System.StringComparison.OrdinalIgnoreCase))
            throw new System.NotSupportedException(name);

        _sourceOpened?.Invoke();
        return new NativeTableCarrierRows<TRow, TMaterializer>(_beforeRows);
    }
}

public sealed class NativeTableCarrierTable(Musoq.Schema.ISchemaColumn[] columns) :
    Musoq.Schema.ISchemaTable
{
    public Musoq.Schema.ISchemaColumn[] Columns { get; } = columns;

    public Musoq.Schema.SchemaTableMetadata Metadata { get; } = new(typeof(object[])) { TypeOrigin = Musoq.Schema.TableSchemaTypeOrigin.TableDefined };

    public Musoq.Schema.ISchemaColumn? GetColumnByName(string name) =>
        System.Linq.Enumerable.SingleOrDefault(Columns, column =>
            string.Equals(column.ColumnName, name, System.StringComparison.OrdinalIgnoreCase));

    public Musoq.Schema.ISchemaColumn[] GetColumnsByName(string name) =>
        System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(Columns, column =>
            string.Equals(column.ColumnName, name, System.StringComparison.OrdinalIgnoreCase)));
}

public sealed class NativeTableCarrierLegacySource(Action? beforeRows = null) :
    Musoq.Schema.DataSources.RowSourceBase<object[]>
{
    protected override void CollectChunks(Musoq.Schema.DataSources.IChunkWriter<object[]> writer)
    {
        beforeRows?.Invoke();
        writer.Write(new[] { new object[] { (short)20 } });
    }
}

public sealed class NativeTableCarrierRows<TRow, TMaterializer>(Action? beforeRows = null) :
    Musoq.Schema.DataSources.RowSourceBase<TRow>
    where TMaterializer : struct, Musoq.Schema.IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(Musoq.Schema.DataSources.IChunkWriter<TRow> writer)
    {
        beforeRows?.Invoke();
        var reader = new NativeTableCarrierReader();
        var row = TMaterializer.Materialize<NativeTableCarrierReader>(ref reader);
        writer.Write(new[] { row });
    }
}

public ref struct NativeTableCarrierReader : Musoq.Schema.IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        if (slot != 0)
            throw new System.ArgumentOutOfRangeException(nameof(slot));

        object value = (short)20;
        return (T)value;
    }
}
