using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionWindowAggregateMode
{
    WholePartition,
    Running,
    BoundedRows
}
