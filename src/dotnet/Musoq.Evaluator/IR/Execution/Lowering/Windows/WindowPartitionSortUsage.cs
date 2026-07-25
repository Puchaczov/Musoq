using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.Windows;

internal sealed class WindowPartitionSortUsage
{
    public HashSet<string> SortedSignatures { get; } = new(StringComparer.Ordinal);

    public bool HasUnsortedConsumer { get; set; }
}
