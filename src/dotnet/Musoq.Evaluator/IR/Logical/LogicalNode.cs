using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical;

public abstract record LogicalNode(OutputSchema OutputSchema)
{
    public abstract IReadOnlyList<LogicalNode> Children { get; }
}
