using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Converter;

internal sealed class InMemorySchema : SchemaBase
{
    private static readonly MethodsAggregator SharedMethodsAggregator = CreateMethodsAggregator();

    private readonly IReadOnlyDictionary<string, InMemorySourceSlot> _slotsBySourceName;
    private readonly IReadOnlyDictionary<string, InMemorySchemaTable> _tablesBySourceName;
    private readonly IReadOnlyDictionary<string, MusoqSourceRows> _bindingsBySourceKey;

    public InMemorySchema(
        string name,
        IReadOnlyList<InMemorySourceSlot> slots,
        IReadOnlyDictionary<string, MusoqSourceRows> bindingsBySourceKey)
        : base(name, SharedMethodsAggregator)
    {
        _slotsBySourceName = slots.ToDictionary(static slot => slot.SourceName, StringComparer.OrdinalIgnoreCase);
        _tablesBySourceName = slots.ToDictionary(
            static slot => slot.SourceName,
            static slot => new InMemorySchemaTable(InMemorySourceShape.For(slot.RowType)),
            StringComparer.OrdinalIgnoreCase);
        _bindingsBySourceKey = bindingsBySourceKey;
    }

    private static MethodsAggregator CreateMethodsAggregator()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (parameters.Length != 0)
            throw new InvalidOperationException($"In-memory source '{name}' does not accept constructor arguments.");

        var slot = GetSlot(name);
        return _tablesBySourceName[slot.SourceName];
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (parameters.Length != 0)
            throw new InvalidOperationException($"In-memory source '{name}' does not accept constructor arguments.");

        var slot = GetSlot(name);
        if (slot.RowType != typeof(T))
        {
            throw new InvalidOperationException(
                $"In-memory source '#{slot.SchemaName}.{slot.SourceName}()' declares row type '{slot.RowType.FullName}', but was requested as '{typeof(T).FullName}'.");
        }

        if (!_bindingsBySourceKey.TryGetValue(slot.Key, out var rows))
            throw new InvalidOperationException($"Rows for in-memory source '#{slot.SchemaName}.{slot.SourceName}()' were not supplied.");

        return new InMemoryChunkSource<T>(rows.GetChunks<T>());
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        return SourcePlanResult.RejectAll(request);
    }

    public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
    {
        return _slotsBySourceName.Values
            .Select(static slot => new SchemaMethodInfo(slot.SourceName, SchemaConstructorInfo.Empty()))
            .ToArray();
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        return GetRawConstructors(metadataContext)
            .Where(constructor => string.Equals(constructor.MethodName, methodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private InMemorySourceSlot GetSlot(string sourceName)
    {
        var normalized = InMemorySourceNames.NormalizeSourceName(sourceName);
        if (_slotsBySourceName.TryGetValue(normalized, out var slot))
            return slot;

        throw new TableNotFoundException($"In-memory source '{sourceName}' is not declared in schema '#{Name}'.");
    }
}
