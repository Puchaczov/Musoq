using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateValueTupleAggregateContext(
    ExecutionVariable RootGroup,
    IReadOnlyList<AggregateGroupLookup> GroupDictionaries,
    ExecutionVariable GroupsToFinalize,
    IReadOnlyList<Type> KeyTypes,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
