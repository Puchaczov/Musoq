namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record CteDefinition(string Name, LogicalNode Plan);
