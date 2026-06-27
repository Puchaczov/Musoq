using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAdaptExpando(
    ExecutionVariable Target,
    ExecutionVariable Source,
    ExpandoAdapterShape Shape) : ExecutionNode;
