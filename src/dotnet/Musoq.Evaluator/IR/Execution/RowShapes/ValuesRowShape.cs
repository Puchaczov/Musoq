namespace Musoq.Evaluator.IR.Execution;

public sealed record ValuesRowShape(
    string Alias,
    GeneratedRowShape GeneratedShape) : RowShape(Alias, GeneratedShape.Fields);
