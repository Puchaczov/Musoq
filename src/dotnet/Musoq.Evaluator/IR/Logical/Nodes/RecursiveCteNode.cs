using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record RecursiveCteNode(
    string Name,
    LogicalNode Anchor,
    LogicalNode RecursiveMember,
    RecursiveCteUnionKind UnionKind,
    string[] Keys,
    int[] IdentityFieldIndexes) : LogicalNode(Anchor.OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Anchor, RecursiveMember];
}
