using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupShape(
    string TypeName,
    IReadOnlyList<AggregateGroupKeyField> Keys,
    IReadOnlyList<AggregateCapturedField> CapturedFields,
    IReadOnlyList<AggregateAccumulatorField> Accumulators,
    IReadOnlyList<AggregateGroupOwnerField> OwnerFields) : RowShape(TypeName, [])
{
    public AggregateGroupShape(
        string typeName,
        IReadOnlyList<AggregateGroupKeyField> keys,
        IReadOnlyList<AggregateCapturedField> capturedFields,
        IReadOnlyList<AggregateAccumulatorField> accumulators)
        : this(typeName, keys, capturedFields, accumulators, [])
    {
    }

    public bool RequiresParentLinks => OwnerFields.Count > 0;
}
