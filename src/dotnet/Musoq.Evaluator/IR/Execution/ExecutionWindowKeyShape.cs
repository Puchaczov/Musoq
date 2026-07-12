using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowKeyShape(
    ExecutionTypeRef ElementType,
    bool IsTyped,
    string? GeneratedElementTypeName = null,
    bool IsGeneratedOrderKey = false,
    IReadOnlyList<ExecutionWindowGeneratedKeyPart>? GeneratedParts = null)
{
    internal ExecutionWindowKeyShape(
        Type elementType,
        bool isTyped,
        string? generatedElementTypeName = null,
        bool isGeneratedOrderKey = false,
        IReadOnlyList<ExecutionWindowGeneratedKeyPart>? generatedParts = null)
        : this(
            ExecutionTypeRef.FromClr(elementType),
            isTyped,
            generatedElementTypeName,
            isGeneratedOrderKey,
            generatedParts)
    {
    }
}
