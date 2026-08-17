using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowKeyShape
{
    public ExecutionWindowKeyShape(
        ExecutionTypeRef ElementType,
        bool IsTyped,
        string? GeneratedElementTypeName = null,
        bool IsGeneratedOrderKey = false,
        IReadOnlyList<ExecutionWindowGeneratedKeyPart>? GeneratedParts = null)
    {
        this.ElementType = ElementType;
        this.IsTyped = IsTyped;
        this.GeneratedElementTypeName = GeneratedElementTypeName;
        this.IsGeneratedOrderKey = IsGeneratedOrderKey;
        this.GeneratedParts = GeneratedParts == null ? null : ExecutionIrCollections.Freeze(GeneratedParts);
    }

    public ExecutionTypeRef ElementType { get; init; }

    public bool IsTyped { get; init; }

    public string? GeneratedElementTypeName { get; init; }

    public bool IsGeneratedOrderKey { get; init; }

    public IReadOnlyList<ExecutionWindowGeneratedKeyPart>? GeneratedParts { get; init; }

    internal ExecutionWindowKeyShape(
        Type elementType,
        bool isTyped,
        string? generatedElementTypeName = null,
        bool isGeneratedOrderKey = false,
        IReadOnlyList<ExecutionWindowGeneratedKeyPart>? generatedParts = null)
        : this(
            ExecutionClrBindingFactory.FromClr(elementType),
            isTyped,
            generatedElementTypeName,
            isGeneratedOrderKey,
            generatedParts)
    {
    }
}
