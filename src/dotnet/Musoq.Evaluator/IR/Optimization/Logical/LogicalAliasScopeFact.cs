namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalAliasScopeFact(
    string ScopePath,
    string[] Aliases,
    string[] DuplicateAliases);

