namespace Musoq.Evaluator.IR.Execution;

public sealed record PositionalAccess(int Index, ExecutionTypeRef? ValueType = null) : FieldAccessStrategy;
