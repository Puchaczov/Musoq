using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record SchemaScanNode(
    string SchemaName,
    string MethodName,
    IrExpression[] Arguments,
    string Alias,
    OutputSchema OutputSchema,
    string? SourceContextId = null,
    Type? SourceConstructionType = null,
    bool SourceConstructionSupportsContext = false,
    string? SourceConstructorStableId = null,
    ExecutionCallableRef? SourceConstructor = null,
    IReadOnlyList<ExecutionStructuralLimitPlan?>? StructuralArgumentLimits = null) : LogicalNode(OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = Array.Empty<LogicalNode>();
}
