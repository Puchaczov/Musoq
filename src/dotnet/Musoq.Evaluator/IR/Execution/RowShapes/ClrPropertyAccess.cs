namespace Musoq.Evaluator.IR.Execution;

public sealed record ClrPropertyAccess(string PropertyName) : FieldAccessStrategy;
