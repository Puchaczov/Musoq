namespace Musoq.Evaluator.IR.Execution;

public sealed record NestedPositionalAccess(int Index, string PropertyPath) : FieldAccessStrategy;
