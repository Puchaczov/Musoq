using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionFusedCteProducer : ExecutionNode
{
    public ExecutionFusedCteProducer(
        IReadOnlyList<ExecutionFusedCteOutput> outputs,
        ExecutionBlock body)
    {
        Outputs = ExecutionIrCollections.Freeze(outputs);
        Body = body;
    }

    public IReadOnlyList<ExecutionFusedCteOutput> Outputs { get; init; }
    public ExecutionBlock Body { get; init; }
}
