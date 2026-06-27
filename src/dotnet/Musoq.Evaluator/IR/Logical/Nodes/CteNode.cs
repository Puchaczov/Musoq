using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record CteNode(CteDefinition[] Definitions, LogicalNode Query) : LogicalNode(Query.OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } =
        Definitions.Select(d => d.Plan).Append(Query).ToArray();
}
