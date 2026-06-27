using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowFrame(
    ExecutionWindowFrameKind Kind,
    ExecutionWindowFrameBound Start,
    ExecutionWindowFrameBound End);
