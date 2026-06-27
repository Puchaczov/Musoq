using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionScopedBlock(ExecutionBlock Body) : ExecutionNode;
