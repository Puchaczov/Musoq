namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteReadOnceFusionCandidate(
    int RelatedTableIndex,
    ExecutionBlock Body) : ExecutionNode;
