using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteSidecarIndexPlan(
    IReadOnlyDictionary<string, IReadOnlyList<CteSidecarIndexSpec>> DefinitionsByName,
    IReadOnlyDictionary<PhysicalHashJoinNode, CteSidecarIndexSpec> ConsumersByJoin)
{
    public static CteSidecarIndexPlan Empty { get; } = new(
        new Dictionary<string, IReadOnlyList<CteSidecarIndexSpec>>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<PhysicalHashJoinNode, CteSidecarIndexSpec>(ReferenceComparer<PhysicalHashJoinNode>.Instance));
}
