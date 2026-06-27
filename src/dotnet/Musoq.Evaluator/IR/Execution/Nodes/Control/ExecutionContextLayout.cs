using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionContextLayout(IReadOnlyList<ExecutionContextSegment> Segments);
