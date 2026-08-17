namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalSourceContextFact(
    string ScopePath,
    string Alias,
    string? SourceContextId,
    string SourceKind);

