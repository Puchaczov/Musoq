using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalCteNode(
    PhysicalCteDefinition[] Definitions,
    PhysicalNode Query) : PhysicalNode(Query.OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } =
        Definitions.Select(d => d.Plan).Append(Query).ToArray();
}
