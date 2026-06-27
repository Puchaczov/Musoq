using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalPropertySourceNode(
    string SourceAlias,
    PropertyFromNode.PropertyNameAndTypePair[] PropertiesChain,
    string Alias,
    int ColumnIndex,
    Type ResultType,
    ApplyKind ApplyKind,
    OutputSchema OutputSchema) : PhysicalNode(OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = Array.Empty<PhysicalNode>();
}
