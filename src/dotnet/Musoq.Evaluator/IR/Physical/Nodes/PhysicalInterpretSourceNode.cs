using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalInterpretSourceNode(
    string SchemaName,
    InterpretSourceKind Kind,
    IrExpression[] Arguments,
    string Alias,
    Type ResultType,
    ApplyKind ApplyKind,
    OutputSchema OutputSchema) : PhysicalNode(OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = Array.Empty<PhysicalNode>();
}
