namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionVariable(
    string Name,
    Type Type,
    string? GeneratedRowTypeName = null);
