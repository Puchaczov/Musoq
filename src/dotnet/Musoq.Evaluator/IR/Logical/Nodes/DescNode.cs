using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record DescNode(
    string SchemaName,
    string MethodName,
    DescType Type,
    string? Column,
    IrExpression[] Arguments,
    string SourceContextId,
    OutputSchema OutputSchema,
    OutputSchema? QueryOutputSchema = null) : LogicalNode(OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = Array.Empty<LogicalNode>();
}
