using Musoq.Evaluator.Tests.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;

using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Tests.Schema.RuntimeDynamic;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;
using RuntimeSchemaColumn = Musoq.Schema.DataSources.SchemaColumn;

namespace Musoq.Evaluator.Tests.Schema.RuntimeDynamic
{

[DynamicObjectPropertyDefaultTypeHint(typeof(double))]
[DynamicObjectPropertyTypeHint("Raw", typeof(ulong))]
public sealed class RuntimeDynamicBranch
{
    private readonly IReadOnlyDictionary<string, object?> _values;
    private readonly ConcurrentDictionary<string, int> _accessCounts = new(StringComparer.Ordinal);

    public RuntimeDynamicBranch(IReadOnlyDictionary<string, object?> values)
    {
        _values = values;
    }

    public int GetAccessCount(string name) =>
        _accessCounts.TryGetValue(name, out var count) ? count : 0;

    public double Measurement => Read<double>(nameof(Measurement));
    public ulong Raw => Read<ulong>(nameof(Raw));

    private T Read<T>(string name)
    {
        _accessCounts.AddOrUpdate(name, 1, static (_, count) => count + 1);
        if (!_values.TryGetValue(name, out var value))
            throw new InvalidOperationException($"Source field '{name}' has no supplied value.");
        return value is null ? default! : (T)value;
    }

}

public sealed class RuntimeDynamicRow
{
    private readonly IReadOnlyDictionary<string, object?> _values;
    private readonly ConcurrentDictionary<string, int> _accessCounts = new(StringComparer.Ordinal);

    public RuntimeDynamicRow(
        string label,
        IReadOnlyDictionary<string, object?> values,
        RuntimeDynamicBranch? staticBranch = null)
    {
        Label = label;
        StaticBranch = staticBranch;
        _values = values;
    }

    public string Label { get; }

    public RuntimeDynamicBranch? StaticBranch { get; }

    public int GetAccessCount(string name) =>
        _accessCounts.TryGetValue(name, out var count) ? count : 0;

    public int RuntimeKey => Read<int>(nameof(RuntimeKey));
    public int AlphaKey => Read<int>(nameof(AlphaKey));
    public int BetaKey => Read<int>(nameof(BetaKey));
    public bool Enabled => Read<bool>(nameof(Enabled));
    public double Metric => Read<double>(nameof(Metric));
    public string Payload => Read<string>(nameof(Payload));
    public RuntimeDynamicBranch? Branch => Read<RuntimeDynamicBranch?>(nameof(Branch));

    private T Read<T>(string name)
    {
        _accessCounts.AddOrUpdate(name, 1, static (_, count) => count + 1);
        if (!_values.TryGetValue(name, out var value))
            throw new InvalidOperationException($"Source field '{name}' has no supplied value.");
        return value is null ? default! : (T)value;
    }

}

public sealed class RuntimeDynamicLookupRow
{
    public RuntimeDynamicLookupRow(int id, double factor)
    {
        Id = id;
        Factor = factor;
    }

    public int Id { get; }

    public double Factor { get; }
}

public sealed class RuntimeMetaObjectProvider : IDynamicMetaObjectProvider
{
    public DynamicMetaObject GetMetaObject(Expression parameter) =>
        throw new NotSupportedException();

    public IEnumerable<string> GetDynamicMemberNames() => [];
}

public sealed class RuntimeDynamicLibrary : LibraryBase
{
    [BindableMethod]
    public double Scale(double value, double factor) => value * factor;
}

public sealed class RuntimeDynamicSchemaProvider : ISchemaProvider
{
    private readonly IReadOnlyList<RuntimeDynamicRow> _rows;
    private readonly IReadOnlyList<RuntimeDynamicLookupRow> _lookupRows;
    private readonly string _runtimeKeyName;

    public RuntimeDynamicSchemaProvider(
        IReadOnlyList<RuntimeDynamicRow>? rows = null,
        IReadOnlyList<RuntimeDynamicLookupRow>? lookupRows = null,
        string runtimeKeyName = "RuntimeKey")
    {
        _rows = rows ?? [];
        _lookupRows = lookupRows ?? [];
        _runtimeKeyName = runtimeKeyName;
    }

    public List<Type> RequestedRowTypes { get; } = [];

    public ISchema GetSchema(string schema) => new RuntimeDynamicSchema(_rows, _lookupRows, RequestedRowTypes, _runtimeKeyName);
}

public sealed class RuntimeDynamicSchema(
    IReadOnlyList<RuntimeDynamicRow> rows,
    IReadOnlyList<RuntimeDynamicLookupRow> lookupRows,
    List<Type> requestedRowTypes,
    string runtimeKeyName)
    : SchemaBase("runtime", CreateMethods())
{
    private readonly ISchemaColumn[] _eventColumns = CreateEventColumns(runtimeKeyName);

    private static readonly ISchemaColumn[] LookupColumns =
    [
        new RuntimeSchemaColumn("Id", 0, typeof(int)),
        new RuntimeSchemaColumn("Factor", 1, typeof(double))
    ];

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new RuntimeDynamicLibrary());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters) =>
        name.Equals("events", StringComparison.OrdinalIgnoreCase)
            ? new RuntimeDynamicTable(_eventColumns, typeof(RuntimeDynamicRow))
            : name.Equals("lookup", StringComparison.OrdinalIgnoreCase)
                ? new RuntimeDynamicTable(LookupColumns, typeof(RuntimeDynamicLookupRow))
                : throw new SchemaNotFoundException();

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext) =>
        methodName.Equals("events", StringComparison.OrdinalIgnoreCase) ||
        methodName.Equals("lookup", StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(methodName, SchemaConstructorInfo.Empty())]
            : [];

    public override RowSource<T> GetRowSource<T, TArguments>(
        string name,
        SourceExecutionContext executionContext,
        TArguments nativeArguments)
    {
        requestedRowTypes.Add(typeof(T));

        if (name.Equals("events", StringComparison.OrdinalIgnoreCase))
        {
            if (typeof(T) != typeof(RuntimeDynamicRow))
                throw new InvalidOperationException($"events requested as {typeof(T).FullName}");

            return (RowSource<T>)(object)new RuntimeDynamicRowSource(rows);
        }

        if (name.Equals("lookup", StringComparison.OrdinalIgnoreCase))
        {
            if (typeof(T) != typeof(RuntimeDynamicLookupRow))
                throw new InvalidOperationException($"lookup requested as {typeof(T).FullName}");

            return (RowSource<T>)(object)new RuntimeDynamicLookupRowSource(lookupRows);
        }

        throw new SchemaNotFoundException();
    }

    private static ISchemaColumn[] CreateEventColumns(string runtimeKeyName)
    {
        if (runtimeKeyName.Equals("RuntimeKey", StringComparison.Ordinal))
        {
            return
            [
                new RuntimeSchemaColumn("Label", 0, typeof(string)),
                new RuntimeSchemaColumn("RuntimeKey", 1, typeof(int)),
                new RuntimeSchemaColumn("Enabled", 2, typeof(bool)),
                new RuntimeSchemaColumn("Metric", 3, typeof(double)),
                new RuntimeSchemaColumn("Payload", 4, typeof(string)),
                new RuntimeSchemaColumn("Branch", 5, typeof(RuntimeDynamicBranch)),
                new RuntimeSchemaColumn("Branch.Measurement", 6, typeof(double)),
                new RuntimeSchemaColumn("Branch.Raw", 7, typeof(ulong)),
                new RuntimeSchemaColumn("StaticBranch", 8, typeof(RuntimeDynamicBranch)),
                new RuntimeSchemaColumn("StaticBranch.Measurement", 9, typeof(double)),
                new RuntimeSchemaColumn("StaticBranch.Raw", 10, typeof(ulong))
            ];
        }

        return
        [
            new RuntimeSchemaColumn("Label", 0, typeof(string)),
            new RuntimeSchemaColumn(runtimeKeyName, 1, typeof(int))
        ];
    }
}

public sealed class RuntimeDynamicTable(ISchemaColumn[] columns, Type entityType) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(entityType);

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.FirstOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

public sealed class RuntimeDynamicRowSource(IReadOnlyList<RuntimeDynamicRow> rows)
    : RowSourceBase<RuntimeDynamicRow>
{
    protected override void CollectChunks(IChunkWriter<RuntimeDynamicRow> writer) => writer.Write(rows);
}

public sealed class RuntimeDynamicLookupRowSource(IReadOnlyList<RuntimeDynamicLookupRow> rows)
    : RowSourceBase<RuntimeDynamicLookupRow>
{
    protected override void CollectChunks(IChunkWriter<RuntimeDynamicLookupRow> writer) => writer.Write(rows);
}
}
