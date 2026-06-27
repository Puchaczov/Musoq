using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowKeyShape(
    Type ElementType,
    bool IsTyped,
    string? GeneratedElementTypeName = null,
    bool IsGeneratedOrderKey = false,
    IReadOnlyList<ExecutionWindowGeneratedKeyPart>? GeneratedParts = null);
