namespace Musoq.Evaluator.IR.Execution;

internal static class SourceGeneratedRowShapeResolver
{
    internal static GeneratedRowShape? Resolve(RowShape sourceShape) => sourceShape switch
    {
        GeneratedRowShape { IsQueryScopedRow: true } queryRowShape => queryRowShape,
        ValuesRowShape valuesShape => valuesShape.GeneratedShape,
        _ => null
    };
}
