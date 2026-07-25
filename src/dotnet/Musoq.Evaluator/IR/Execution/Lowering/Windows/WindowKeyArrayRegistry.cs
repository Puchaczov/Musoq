using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.Windows;

internal sealed class WindowKeyArrayRegistry
{
    private readonly Dictionary<string, ExecutionWindowKeyArray> _arrays = new(StringComparer.Ordinal);

    public ExecutionWindowKeyArray GetOrAdd(
        string signature,
        ExecutionVariable candidate,
        ExecutionWindowKeyShape? shape = null,
        bool shouldMaterialize = true)
    {
        if (_arrays.TryGetValue(signature, out var array))
            return array with { ShouldExtract = false };

        var created = new ExecutionWindowKeyArray(candidate, true, shape, shouldMaterialize);
        _arrays.Add(signature, created);
        return created;
    }
}
