using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteSidecarIndexPlan(
    IReadOnlyDictionary<string, IReadOnlyList<CteSidecarIndexSpec>> DefinitionsByName,
    IReadOnlyDictionary<PhysicalHashJoinNode, CteSidecarIndexSpec> ConsumersByJoin)
{
    public static CteSidecarIndexPlan Empty { get; } = new(
        new Dictionary<string, IReadOnlyList<CteSidecarIndexSpec>>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<PhysicalHashJoinNode, CteSidecarIndexSpec>(ReferenceComparer<PhysicalHashJoinNode>.Instance));
}
