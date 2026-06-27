using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowGeneratedKeyPart(
    Type Type,
    bool Descending,
    NullOrdering NullOrdering = NullOrdering.Default);
