namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionCapacityHint? CreateRowsCapacityCandidate(
        ExecutionVariable target,
        ExecutionExpression rows)
    {
        return ExecutionCapacityHintCandidates.CreateRowsCandidate(target, rows);
    }
}
