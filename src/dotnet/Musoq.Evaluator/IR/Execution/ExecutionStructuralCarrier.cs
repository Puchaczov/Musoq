namespace Musoq.Evaluator.IR.Execution;

/// <summary>A compiler-selected native structural carrier with an exact recursive field shape.</summary>
public sealed record ExecutionStructuralCarrier
{
    public ExecutionStructuralCarrier(int definitionIndex, ExecutionStructuralShape shape)
    {
        if (definitionIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(definitionIndex));
        DefinitionIndex = definitionIndex;
        Shape = shape ?? throw new ArgumentNullException(nameof(shape));
    }

    public int DefinitionIndex { get; }
    public ExecutionStructuralShape Shape { get; }
}
