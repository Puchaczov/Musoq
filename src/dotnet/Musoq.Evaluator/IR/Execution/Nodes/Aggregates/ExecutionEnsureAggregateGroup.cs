namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionEnsureAggregateGroup(
    ExecutionVariable RootGroup,
    ExecutionVariable CurrentGroup,
    ExecutionVariable Groups,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
