namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionCapacityHint? CreateCteSidecarCapacityCandidate(
        ExecutionCapacityHint? capacityHint,
        ExecutionVariable index)
    {
        return capacityHint switch
        {
            ExecutionRowsCapacityHintCandidate candidate => candidate with { Target = index },
            _ => capacityHint
        };
    }
}
