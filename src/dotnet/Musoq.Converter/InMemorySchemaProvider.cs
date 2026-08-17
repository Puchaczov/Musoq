using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Converter;

internal sealed class InMemorySchemaProvider : ISchemaProvider
{
    private readonly IReadOnlyDictionary<string, InMemorySchema> _schemas;

    private InMemorySchemaProvider(IReadOnlyDictionary<string, InMemorySchema> schemas)
    {
        _schemas = schemas;
    }

    public static InMemorySchemaProvider Create(IReadOnlyList<InMemorySourceSlot> slots)
    {
        return Create(slots, []);
    }

    public static InMemorySchemaProvider Create(
        IReadOnlyList<InMemorySourceSlot> slots,
        IReadOnlyList<MusoqSourceRows> rows)
    {
        ArgumentNullException.ThrowIfNull(slots);
        ArgumentNullException.ThrowIfNull(rows);

        var bindings = CreateBindingMap(slots, rows);
        var schemas = slots
            .GroupBy(static slot => slot.SchemaName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                group => new InMemorySchema(group.Key, group.ToArray(), bindings),
                StringComparer.OrdinalIgnoreCase);

        return new InMemorySchemaProvider(schemas);
    }

    public ISchema GetSchema(string schema)
    {
        var schemaName = InMemorySourceNames.NormalizeSchemaName(schema);
        if (_schemas.TryGetValue(schemaName, out var inMemorySchema))
            return inMemorySchema;

        throw new SourceNotFoundException($"In-memory schema '#{schemaName}' is not declared.");
    }

    private static Dictionary<string, MusoqSourceRows> CreateBindingMap(
        IReadOnlyList<InMemorySourceSlot> slots,
        IReadOnlyList<MusoqSourceRows> rows)
    {
        var declared = slots.ToDictionary(static slot => slot.Key, StringComparer.OrdinalIgnoreCase);
        var bindings = new Dictionary<string, MusoqSourceRows>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceRows in rows)
        {
            var key = InMemorySourceNames.CreateKey(sourceRows.SchemaName, sourceRows.SourceName);
            if (!declared.TryGetValue(key, out var slot))
                throw new InvalidOperationException($"Rows were supplied for undeclared source '#{sourceRows.SchemaName}.{sourceRows.SourceName}()'.");

            if (!slot.RowType.IsAssignableFrom(sourceRows.RowType))
            {
                throw new InvalidOperationException(
                    $"Rows for '#{sourceRows.SchemaName}.{sourceRows.SourceName}()' have type '{sourceRows.RowType.FullName}', but the source was declared as '{slot.RowType.FullName}'.");
            }

            if (!bindings.TryAdd(key, sourceRows))
                throw new InvalidOperationException($"Rows for source '#{sourceRows.SchemaName}.{sourceRows.SourceName}()' were supplied more than once.");
        }

        return bindings;
    }
}
