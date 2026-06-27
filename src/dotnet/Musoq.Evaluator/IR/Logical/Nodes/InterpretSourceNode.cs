using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record InterpretSourceNode(
    string SchemaName,
    InterpretSourceKind Kind,
    IrExpression[] Arguments,
    string Alias,
    Type ResultType,
    ApplyKind ApplyKind,
    OutputSchema OutputSchema) : LogicalNode(OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = Array.Empty<LogicalNode>();
}
