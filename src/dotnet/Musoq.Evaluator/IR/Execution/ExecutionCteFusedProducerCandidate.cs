using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteFusedProducerCandidate(
    IReadOnlyList<ExecutionFusedCteOutput> Outputs,
    ExecutionBlock Body) : ExecutionNode;
