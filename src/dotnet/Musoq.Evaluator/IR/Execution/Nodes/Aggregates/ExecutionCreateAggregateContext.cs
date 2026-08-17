namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateAggregateContext(
    ExecutionVariable RootGroup,
    ExecutionVariable CurrentGroup,
    ExecutionVariable Groups,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
