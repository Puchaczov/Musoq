namespace Musoq.Evaluator.IR.Bindings;

public sealed record ColumnSchema(
    string Name,
    Type Type,
    int Index,
    string? IntendedTypeName = null);
