using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateSingleKeyAggregateContext(
    ExecutionVariable RootGroup,
    ExecutionVariable Groups,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable? NullGroup,
    ExecutionTypeRef KeyType,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    internal ExecutionCreateSingleKeyAggregateContext(
        ExecutionVariable rootGroup,
        ExecutionVariable groups,
        ExecutionVariable groupsToFinalize,
        ExecutionVariable? nullGroup,
        Type keyType,
        AggregateGroupPlan groupPlan)
        : this(rootGroup, groups, groupsToFinalize, nullGroup, ExecutionTypeRef.FromClr(keyType), groupPlan)
    {
    }

    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
