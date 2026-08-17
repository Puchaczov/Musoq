namespace Musoq.Evaluator.IR.Execution;

public sealed record ReflectedMemberAccess(string PropertyPath) : FieldAccessStrategy;
