namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionSingleUsePipelineFusionCandidate(
    int RelatedTableIndex,
    ExecutionBlock Body) : ExecutionNode;
