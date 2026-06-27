using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionFusedCteProducer(
    IReadOnlyList<ExecutionFusedCteOutput> Outputs,
    ExecutionBlock Body) : ExecutionNode;
