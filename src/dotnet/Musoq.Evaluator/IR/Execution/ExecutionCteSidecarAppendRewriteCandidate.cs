using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarAppendRewriteCandidate(
    ExecutionAppendRow AppendRow,
    IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> Indexes) : ExecutionNode;
