using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalDescNode(
    string SchemaName,
    string MethodName,
    DescType Type,
    string? Column,
    IrExpression[] Arguments,
    string SourceContextId,
    OutputSchema OutputSchema,
    OutputSchema? QueryOutputSchema = null,
    TextSpan? ColumnSpan = null) : PhysicalNode(OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = Array.Empty<PhysicalNode>();
}
