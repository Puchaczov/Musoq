using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalSchemaScanNode(
    string SchemaName,
    string MethodName,
    IrExpression[] Arguments,
    string Alias,
    IrExpression[] PushedPredicates,
    string[] ProjectedColumns,
    OutputSchema OutputSchema,
    string? SourceContextId = null,
    SourceTransferStrategyPlan? SourceTransferStrategy = null,
    Type? SourceConstructionType = null,
    bool SourceConstructionSupportsContext = false,
    string? SourceConstructorStableId = null,
    ExecutionCallableRef? SourceConstructor = null, IReadOnlyList<ExecutionStructuralLimitPlan?>? StructuralArgumentLimits = null) : PhysicalNode(OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = Array.Empty<PhysicalNode>();
}
