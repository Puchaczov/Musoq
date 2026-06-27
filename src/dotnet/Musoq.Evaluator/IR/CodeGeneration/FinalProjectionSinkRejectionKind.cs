namespace Musoq.Evaluator.IR.CodeGeneration;

public enum FinalProjectionSinkRejectionKind
{
    None,
    Unknown,
    ExpectedOneSourceScan,
    FinalReturnedTableMismatch,
    ExpectedOneProjectionLoop,
    UnexpectedPlanNodes,
    ProjectionAppendMissing,
    ProjectionAppendTargetMissing,
    ProjectionLoopMismatch,
    UnsupportedPostOperationChain
}
