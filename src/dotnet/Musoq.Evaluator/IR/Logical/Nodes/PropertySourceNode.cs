using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record PropertySourceNode(
    string SourceAlias,
    PropertyFromNode.PropertyNameAndTypePair[] PropertiesChain,
    string Alias,
    int ColumnIndex,
    Type ResultType,
    ApplyKind ApplyKind,
    OutputSchema OutputSchema) : LogicalNode(OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = Array.Empty<LogicalNode>();
}
