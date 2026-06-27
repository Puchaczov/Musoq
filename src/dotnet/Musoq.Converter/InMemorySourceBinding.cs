using System.Collections.Generic;
using System.Linq;

namespace Musoq.Converter;

internal sealed class InMemorySourceBinding
{
    private readonly InMemorySourceSlot[] _slots;
    private readonly Type[] _additionalReferenceTypes;

    public InMemorySourceBinding(IEnumerable<InMemorySourceSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        _slots = slots.ToArray();
        _additionalReferenceTypes = _slots.Select(static slot => slot.RowType).ToArray();
    }

    public IReadOnlyList<InMemorySourceSlot> Slots => _slots;

    public IReadOnlyList<Type> AdditionalReferenceTypes => _additionalReferenceTypes;

    public InMemorySchemaProvider CreateMetadataProvider()
    {
        return InMemorySchemaProvider.Create(_slots);
    }

    public InMemorySchemaProvider CreateRuntimeProvider(IReadOnlyList<MusoqSourceRows> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return InMemorySchemaProvider.Create(_slots, rows);
    }

    public InMemorySchemaProvider CreateRuntimeProvider(params MusoqSourceRows[] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return CreateRuntimeProvider((IReadOnlyList<MusoqSourceRows>)rows);
    }

    public MusoqSourceRows[] SnapshotDefaultRows(IEnumerable<MusoqSourceRows> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows.ToArray();
    }
}
