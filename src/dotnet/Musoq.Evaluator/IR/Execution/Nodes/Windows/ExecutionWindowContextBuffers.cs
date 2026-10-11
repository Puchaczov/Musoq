using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Compilation-selected concrete storage consumed by a direct typed window context operation.</summary>
public sealed record ExecutionWindowContextBuffers
{
    public ExecutionWindowContextBuffers(ExecutionVariable values, IReadOnlyList<ExecutionVariable> arguments,
        ExecutionVariable? rangeKeys, ExecutionVariable? orderKeyValues, ExecutionTypeRef contextType)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(contextType);
        Values = values;
        Arguments = ExecutionIrCollections.Freeze(arguments);
        RangeKeys = rangeKeys;
        OrderKeyValues = orderKeyValues;
        ContextType = contextType;
    }

    public ExecutionVariable Values { get; }
    public IReadOnlyList<ExecutionVariable> Arguments { get; }
    public ExecutionVariable? RangeKeys { get; }
    public ExecutionVariable? OrderKeyValues { get; }
    public ExecutionTypeRef ContextType { get; }
}
