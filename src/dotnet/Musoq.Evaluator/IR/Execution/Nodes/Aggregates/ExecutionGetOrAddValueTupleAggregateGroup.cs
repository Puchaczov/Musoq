using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionGetOrAddValueTupleAggregateGroup(
    ExecutionVariable RootGroup,
    IReadOnlyList<AggregateGroupLookup> GroupDictionaries,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable Group,
    IReadOnlyList<ExecutionExpression> Keys,
    IReadOnlyList<string> KeyNames,
    IReadOnlyList<Type> KeyTypes,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
