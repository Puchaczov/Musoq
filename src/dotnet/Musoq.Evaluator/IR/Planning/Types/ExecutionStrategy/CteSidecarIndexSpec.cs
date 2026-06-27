using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteSidecarIndexSpec(
    string CteName,
    CteSidecarIndexKind Kind,
    string[] KeyColumns,
    Type KeyType,
    int IndexSlot)
{
    public string StableKey => CteSidecarIndexPlanner.CreateStableKey(CteName, Kind, KeyColumns);
}
