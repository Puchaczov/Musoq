using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.Windows;

internal sealed class WindowPartitionSetRegistry
{
    private readonly Dictionary<string, ExecutionVariable> _variables = new(StringComparer.Ordinal);

    public ExecutionWindowPartitionSet GetOrAdd(
        string signature,
        ExecutionVariable candidate,
        bool sortInPlace = false)
    {
        if (_variables.TryGetValue(signature, out var variable))
            return new ExecutionWindowPartitionSet(variable, false, sortInPlace);

        _variables.Add(signature, candidate);
        return new ExecutionWindowPartitionSet(candidate, true, sortInPlace);
    }
}
