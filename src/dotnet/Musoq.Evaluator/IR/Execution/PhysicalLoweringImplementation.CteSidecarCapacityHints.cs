namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
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
