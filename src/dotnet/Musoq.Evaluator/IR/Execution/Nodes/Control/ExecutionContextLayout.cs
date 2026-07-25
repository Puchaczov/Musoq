using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionContextLayout
{
    public ExecutionContextLayout(IReadOnlyList<ExecutionContextSegment> segments)
    {
        Segments = ExecutionIrCollections.Freeze(segments);
    }

    public IReadOnlyList<ExecutionContextSegment> Segments { get; init; }
}
