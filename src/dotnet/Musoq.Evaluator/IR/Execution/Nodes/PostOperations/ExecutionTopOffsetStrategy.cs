using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionTopOffsetStrategy
{
    BoundedHeap,
    OrderedSlice
}
