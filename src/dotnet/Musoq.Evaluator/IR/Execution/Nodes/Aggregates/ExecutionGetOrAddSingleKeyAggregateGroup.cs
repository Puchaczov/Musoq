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
    Type KeyType,
    ExecutionVariable? NullGroup,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
