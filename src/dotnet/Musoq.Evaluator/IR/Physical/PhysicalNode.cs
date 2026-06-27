using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical;

public abstract record PhysicalNode(OutputSchema OutputSchema)
{
    public abstract IReadOnlyList<PhysicalNode> Children { get; }
}
