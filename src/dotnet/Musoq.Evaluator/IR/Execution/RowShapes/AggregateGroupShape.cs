using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupShape : RowShape
{
    public AggregateGroupShape(
        string typeName,
        IReadOnlyList<AggregateGroupKeyField> keys,
        IReadOnlyList<AggregateCapturedField> capturedFields,
        IReadOnlyList<AggregateAccumulatorField> accumulators,
        IReadOnlyList<AggregateGroupOwnerField> ownerFields)
        : base(typeName, [])
    {
        TypeName = typeName;
        Keys = ExecutionIrCollections.Freeze(keys);
        CapturedFields = ExecutionIrCollections.Freeze(capturedFields);
        Accumulators = ExecutionIrCollections.Freeze(accumulators);
        OwnerFields = ExecutionIrCollections.Freeze(ownerFields);
    }

    public string TypeName { get; init; }

    public IReadOnlyList<AggregateGroupKeyField> Keys { get; init; }

    public IReadOnlyList<AggregateCapturedField> CapturedFields { get; init; }

    public IReadOnlyList<AggregateAccumulatorField> Accumulators { get; init; }

    public IReadOnlyList<AggregateGroupOwnerField> OwnerFields { get; init; }

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
