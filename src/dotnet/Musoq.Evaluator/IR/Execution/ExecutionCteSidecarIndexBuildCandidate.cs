using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarIndexBuildCandidate(
    IReadOnlyList<ExecutionCteSidecarIndexCreateSpec> Indexes) : ExecutionNode;
