using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal sealed record CoupledSourceDefinition(
    SchemaMethodFromNode SchemaMethodNode,
    string? TableName,
    string? ProfileName);
