using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class OptimizationTrace
{
    private readonly List<OptimizationTraceEntry> _entries = [];

    public IReadOnlyList<OptimizationTraceEntry> Entries => _entries;

    internal void Add(OptimizationTraceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }
}
