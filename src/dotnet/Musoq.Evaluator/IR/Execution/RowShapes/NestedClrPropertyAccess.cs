namespace Musoq.Evaluator.IR.Execution;

public sealed record NestedClrPropertyAccess(string PropertyPath) : FieldAccessStrategy;
