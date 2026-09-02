using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

public sealed class EnumQueryRowsSchemaProvider : ISchemaProvider
{
    public EnumQueryRowsSchemaProvider(
        SourceTransferCapabilities capabilities = SourceTransferCapabilities.QueryScopedRows |
                                                  SourceTransferCapabilities.LogicalScalarReads,
        bool corruptDescriptor = false)
    {
        Schema = new EnumQueryRowsSchema(capabilities, corruptDescriptor);
    }

    public EnumQueryRowsSchema Schema { get; }

    public ISchema GetSchema(string schema)
    {
        return string.Equals(schema, "enumrows", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(schema, "#enumrows", StringComparison.OrdinalIgnoreCase)
            ? Schema
            : throw new NotSupportedException(schema);
    }
}

public sealed class EnumQueryRowsSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    private static readonly EnumQueryRowInput[] Inputs =
    [
        new("Running", null, "ReadWrite", null),
        new(null, 10, null, 1),
        new(null, 99, null, 8)
    ];

    private readonly SourceTransferCapabilities _capabilities;
    private readonly bool _corruptDescriptor;

    public SourcePlanRequest? LastPlanRequest { get; private set; }

    public IReadOnlyDictionary<string, string> FrozenEnumFingerprints => _frozenEnumFingerprints;

    private readonly Dictionary<string, string> _frozenEnumFingerprints = new(StringComparer.Ordinal);

    public bool DriftAtExecution { get; set; }

    public EnumQueryRowsSchema(SourceTransferCapabilities capabilities, bool corruptDescriptor)
        : base("enumrows", CreateLibrary())
    {
        _capabilities = capabilities;
        _corruptDescriptor = corruptDescriptor;
        AddTable<DynamicEnumQueryRowsTable>("dynamic");
        AddSource<DynamicEnumLegacySource>("dynamic");
        AddTable<NativeEnumQueryRowsTable>("native");
        AddSource<NativeEnumLegacySource>("native");
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "dynamic", StringComparison.OrdinalIgnoreCase) &&
            metadataContext.AllColumns.Count > 0)
        {
            return new DeclaredEnumQueryRowsTable(metadataContext.AllColumns.ToArray());
        }

        return base.GetTableByName(name, metadataContext, parameters);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters);
        var columns = string.Equals(name, "dynamic", StringComparison.OrdinalIgnoreCase) &&
                      context.MetadataContext.AllColumns.Count > 0
            ? context.MetadataContext.AllColumns.ToArray()
            : descriptor.Columns;

        var firstEnum = columns.FirstOrDefault(static column => column.EnumType != null);
        foreach (var enumColumn in columns.Where(static column => column.EnumType != null))
            _frozenEnumFingerprints[enumColumn.ColumnName] = enumColumn.EnumType!.Fingerprint;

        if (_corruptDescriptor && firstEnum?.EnumType is { } enumType)
        {
            var corrupted = new EnumTypeDescriptor(
                enumType.DisplayName,
                enumType.Origin,
                enumType.UnderlyingKind,
                enumType.IsFlags,
                enumType.Members.Concat(
                [
                    new EnumMemberDescriptor("Corrupted", CreateOne(enumType.UnderlyingKind))
                ]).ToArray());
            columns = columns.Select(column => ReferenceEquals(column, firstEnum)
                    ? new SchemaColumn(
                        column.ColumnName,
                        column.ColumnIndex,
                        column.ColumnType,
                        column.SourceReadType,
                        corrupted)
                    : column)
                .ToArray();
        }

        return descriptor with
        {
            Columns = columns,
            TransferCapabilities = _capabilities
        };
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        LastPlanRequest = request;
        return SourcePlanResult.AcceptAll(request);
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        if (DriftAtExecution && request.Shape.Fields.Any(static field => field.EnumType != null))
            throw new InvalidOperationException("The enum descriptor changed after compilation; recompile the query.");

        foreach (var field in request.Shape.Fields.Where(static field => field.EnumType != null))
            Assert.AreEqual(_frozenEnumFingerprints[field.Name], field.EnumType!.Fingerprint);

        var native = string.Equals(name, "native", StringComparison.OrdinalIgnoreCase);
        return new EnumQueryRowsMaterializedSource<TRow, TMaterializer>(Inputs, request.Shape.Fields, native);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methods);
    }

    private static EnumScalarValue CreateOne(EnumUnderlyingKind kind)
    {
        return EnumScalarValue.FromRaw(kind, 1);
    }
}

public sealed class DeclaredEnumQueryRowsTable(ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(typeof(DynamicEnumQueryRowEntity));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal)).ToArray();
}

public sealed class DynamicEnumQueryRowsTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn("Status", 0, typeof(int)),
        new SchemaColumn("Access", 1, typeof(uint))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(DynamicEnumQueryRowEntity));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal)).ToArray();
}

public sealed class NativeEnumQueryRowsTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn("Status", 0, typeof(NativeQueryStatus)),
        new SchemaColumn("Access", 1, typeof(NativeQueryAccess)),
        new SchemaColumn("OptionalStatus", 2, typeof(NativeQueryStatus?))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(NativeEnumQueryRowEntity));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal)).ToArray();
}

public sealed class DynamicEnumLegacySource : RowSourceBase<DynamicEnumQueryRowEntity>
{
    protected override void CollectChunks(IChunkWriter<DynamicEnumQueryRowEntity> writer) => writer.Write([]);
}

public sealed class NativeEnumLegacySource : RowSourceBase<NativeEnumQueryRowEntity>
{
    protected override void CollectChunks(IChunkWriter<NativeEnumQueryRowEntity> writer) => writer.Write([]);
}

public sealed class EnumQueryRowsMaterializedSource<TRow, TMaterializer>(
    IReadOnlyList<EnumQueryRowInput> inputs,
    IReadOnlyList<QueryRowField> fields,
    bool native) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var rows = new List<TRow>(inputs.Count);
        foreach (var input in inputs)
        {
            var reader = new EnumQueryRowReader(input, fields, native);
            rows.Add(TMaterializer.Materialize<EnumQueryRowReader>(ref reader));
        }

        writer.Write(rows);
    }
}

public struct EnumQueryRowReader(
    EnumQueryRowInput input,
    IReadOnlyList<QueryRowField> fields,
    bool native) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var field = fields[slot];
        var value = field.Name switch
        {
            "Status" => Resolve(input.StatusName, input.StatusNumber, field),
            "OptionalStatus" => Resolve(input.StatusName, input.StatusNumber, field),
            "Access" => Resolve(input.AccessName, input.AccessNumber, field),
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

        return native ? ReadNative<T>(field.Name, value) : ReadCarrier<T>(value);
    }

    private static EnumScalarValue Resolve(
        string? name,
        ulong? number,
        QueryRowField field)
    {
        var descriptor = field.EnumType ??
                         throw new InvalidOperationException($"Field '{field.Name}' lost its enum descriptor.");
        if (name != null)
        {
            return descriptor.TryGetValue(name, out var named)
                ? named
                : throw new InvalidOperationException($"Unknown enum member '{name}'.");
        }

        return EnumScalarValue.FromRaw(descriptor.UnderlyingKind, number ?? 0);
    }

    private static T ReadNative<T>(string name, EnumScalarValue value)
    {
        if (name == "Status" && typeof(T) == typeof(NativeQueryStatus))
        {
            var nativeValue = (NativeQueryStatus)value.AsInt16();
            return Unsafe.As<NativeQueryStatus, T>(ref nativeValue);
        }

        if (name == "OptionalStatus" && typeof(T) == typeof(NativeQueryStatus?))
        {
            NativeQueryStatus? nativeValue = (NativeQueryStatus)value.AsInt16();
            return Unsafe.As<NativeQueryStatus?, T>(ref nativeValue);
        }

        if (name == "Access" && typeof(T) == typeof(NativeQueryAccess))
        {
            var nativeValue = (NativeQueryAccess)value.AsUInt32();
            return Unsafe.As<NativeQueryAccess, T>(ref nativeValue);
        }

        throw new InvalidOperationException($"Unexpected native enum read type '{typeof(T)}'.");
    }

    private static T ReadCarrier<T>(EnumScalarValue value)
    {
        switch (value.Kind)
        {
            case EnumUnderlyingKind.Byte:
                return Reinterpret<byte, T>(value.AsByte());
            case EnumUnderlyingKind.SByte:
                return Reinterpret<sbyte, T>(value.AsSByte());
            case EnumUnderlyingKind.Int16:
                return Reinterpret<short, T>(value.AsInt16());
            case EnumUnderlyingKind.UInt16:
                return Reinterpret<ushort, T>(value.AsUInt16());
            case EnumUnderlyingKind.Int32:
                return Reinterpret<int, T>(value.AsInt32());
            case EnumUnderlyingKind.UInt32:
                return Reinterpret<uint, T>(value.AsUInt32());
            case EnumUnderlyingKind.Int64:
                return Reinterpret<long, T>(value.AsInt64());
            case EnumUnderlyingKind.UInt64:
                return Reinterpret<ulong, T>(value.AsUInt64());
            default:
                throw new InvalidOperationException($"Unsupported enum backing kind '{value.Kind}'.");
        }
    }

    private static TTo Reinterpret<TFrom, TTo>(TFrom value)
        where TFrom : struct
    {
        if (typeof(TFrom) == typeof(TTo))
            return Unsafe.As<TFrom, TTo>(ref value);

        if (typeof(TTo) == typeof(TFrom?))
        {
            TFrom? nullable = value;
            return Unsafe.As<TFrom?, TTo>(ref nullable);
        }

        throw new InvalidOperationException($"Expected logical scalar read '{typeof(TFrom)}' or its nullable form, received '{typeof(TTo)}'.");
    }
}

public readonly record struct EnumQueryRowInput(
    string? StatusName,
    ulong? StatusNumber,
    string? AccessName,
    ulong? AccessNumber);

public sealed class DynamicEnumQueryRowEntity
{
    public int Status { get; init; }

    public uint Access { get; init; }
}

public sealed class NativeEnumQueryRowEntity
{
    public NativeQueryStatus Status { get; init; }

    public NativeQueryAccess Access { get; init; }

    public NativeQueryStatus? OptionalStatus { get; init; }
}

public enum NativeQueryStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}

[Flags]
public enum NativeQueryAccess : uint
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}
