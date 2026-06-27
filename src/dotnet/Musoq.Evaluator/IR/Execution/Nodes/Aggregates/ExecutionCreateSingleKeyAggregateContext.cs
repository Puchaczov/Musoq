using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateSingleKeyAggregateContext(
    ExecutionVariable RootGroup,
    ExecutionVariable Groups,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable? NullGroup,
    Type KeyType,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
