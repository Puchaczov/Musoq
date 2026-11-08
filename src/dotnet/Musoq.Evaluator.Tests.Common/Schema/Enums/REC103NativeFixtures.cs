using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.QueryRows;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;


public enum REC103NativeStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}

[Flags]
public enum REC103NativeAccess : uint
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}

public sealed class REC103MetadataSchemaProvider : ISchemaProvider
{
    public REC103MetadataSchema Schema { get; } = new();

    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "rec103", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return Schema;
    }
}

public sealed class REC103MetadataSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    public REC103MetadataSchema()
        : base("rec103", new MethodsAggregator(new MethodsManager()))
    {
    }

    public List<ISchemaColumn[]> CapturedColumns { get; } = [];

    public bool RowSourceRequested { get; private set; }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters);
        return descriptor with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows |
                                   SourceTransferCapabilities.LogicalScalarReads
        };
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        return string.Equals(methodName, "native", StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(methodName, SchemaConstructorInfo.Empty())]
            : [];
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        var columns = metadataContext.AllColumns.ToArray();
        CapturedColumns.Add(columns);
        return new REC103MetadataTable(columns);
    }

    public override RowSource<T> GetRowSource<T, TArguments>(
        string name,
        SourceExecutionContext executionContext,
        TArguments nativeArguments)
    {
        RowSourceRequested = true;
        throw new NotSupportedException("REC-103 metadata-only provider must not execute rows.");
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
        string name,
        QueryScopedRowSourceRequest request,
        TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, global::Musoq.Schema.ISourceArguments
    {
        RowSourceRequested = true;
        return new EmptyQueryScopedRowSource<TRow>();
    }

    private sealed class EmptyQueryScopedRowSource<TRow> : RowSource<TRow>
    {
        public override IEnumerable<IReadOnlyList<TRow>> Chunks => [];
    }
}

public sealed class REC103MetadataTable(ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(typeof(REC103NativeEntity)) { TypeOrigin = TableSchemaTypeOrigin.TableDefined };

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

public sealed record REC103NativeEntity(
    REC103NativeStatus Status,
    REC103NativeAccess Access,
    REC103NativeStatus? OptionalStatus,
    REC103NativeAccess? OptionalAccess,
    int Number);
