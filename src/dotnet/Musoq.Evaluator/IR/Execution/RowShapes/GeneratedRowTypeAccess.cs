namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedRowTypeAccess(string TypeName, string FieldName) : FieldAccessStrategy;
