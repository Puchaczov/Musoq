using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionWindowKernelPlanStrategy
{
    NoPartition,
    HashPartitionPerPartitionSort,
    GlobalSort,
    AlreadySortedSource
}
