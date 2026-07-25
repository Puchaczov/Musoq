using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalRecursiveCteNode(
    string Name,
    PhysicalNode Anchor,
    PhysicalNode RecursiveMember,
    RecursiveCteUnionKind UnionKind,
    string[] Keys,
    int[] IdentityFieldIndexes,
    PhysicalRecursiveCteInvariantDefinition[] Invariants) : PhysicalNode(Anchor.OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children =>
        [Anchor, ..Invariants.Select(static invariant => invariant.Plan), RecursiveMember];
}
