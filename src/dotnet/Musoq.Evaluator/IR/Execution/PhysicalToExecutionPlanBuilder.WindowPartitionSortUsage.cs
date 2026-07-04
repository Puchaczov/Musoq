using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class WindowPartitionSortUsage
    {
        public HashSet<string> SortedSignatures { get; } = new(StringComparer.Ordinal);

        public bool HasUnsortedConsumer { get; set; }
    }
}
