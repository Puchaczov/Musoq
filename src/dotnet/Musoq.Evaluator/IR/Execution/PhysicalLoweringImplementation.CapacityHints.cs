namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionCapacityHint? CreateRowsCapacityCandidate(
        ExecutionVariable target,
        ExecutionExpression rows)
    {
        return ExecutionCapacityHintCandidates.CreateRowsCandidate(target, rows);
    }
}
