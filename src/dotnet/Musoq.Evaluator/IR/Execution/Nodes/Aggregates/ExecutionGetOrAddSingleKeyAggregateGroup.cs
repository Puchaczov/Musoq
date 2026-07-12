using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionGetOrAddSingleKeyAggregateGroup(
    ExecutionVariable RootGroup,
    ExecutionVariable Groups,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable Group,
    ExecutionExpression Key,
    string KeyName,
    ExecutionTypeRef KeyType,
    ExecutionVariable? NullGroup,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    internal ExecutionGetOrAddSingleKeyAggregateGroup(
        ExecutionVariable rootGroup,
        ExecutionVariable groups,
        ExecutionVariable groupsToFinalize,
        ExecutionVariable group,
        ExecutionExpression key,
        string keyName,
        Type keyType,
        ExecutionVariable? nullGroup,
        AggregateGroupPlan groupPlan)
        : this(rootGroup, groups, groupsToFinalize, group, key, keyName, ExecutionTypeRef.FromClr(keyType), nullGroup, groupPlan)
    {
    }

    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
