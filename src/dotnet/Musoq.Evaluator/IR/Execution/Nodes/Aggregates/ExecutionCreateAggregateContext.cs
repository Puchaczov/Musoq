using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateAggregateContext(
    ExecutionVariable RootGroup,
    ExecutionVariable CurrentGroup,
    ExecutionVariable Groups,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
