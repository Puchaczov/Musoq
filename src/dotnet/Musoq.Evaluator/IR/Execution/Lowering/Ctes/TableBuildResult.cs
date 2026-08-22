using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed record TableBuildResult(
    bool IsBuilt,
    IReadOnlyList<RowShape> Shapes,
    IReadOnlyList<ExecutionNode> Nodes,
    ExecutionVariable Table,
    GeneratedRowShape RowShape,
    FinalShapeResult? FinalResult,
    string UnsupportedReason)
{
    public IReadOnlyList<ApplyPredicateMovementPlan> LoweredApplyPredicateMovementPlans { get; init; } = [];

    public static TableBuildResult Success(
        IReadOnlyList<RowShape> shapes,
        IReadOnlyList<ExecutionNode> nodes,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        return new TableBuildResult(
            true,
            shapes,
            nodes,
            table,
            rowShape,
            new FinalShapeResult(
                table.Name,
                table,
                rowShape,
                new ExecutionColumnMetadata(
                    table.Name,
                    rowShape.Fields
                        .Select(static field => ExecutionColumnMetadataFields.FromFieldBinding(field))
                        .ToArray(),
                    ExecutionColumnMetadataKind.TableColumns)),
            string.Empty);
    }

    public static TableBuildResult Unsupported(string reason)
    {
        return new TableBuildResult(
            false,
            [],
            [],
            new ExecutionVariable(string.Empty, typeof(object)),
            new GeneratedRowShape(string.Empty, []),
            null,
            reason);
    }
}

internal sealed record LoweredTable(
    IReadOnlyList<RowShape> Shapes,
    IReadOnlyList<ExecutionNode> Nodes,
    ExecutionVariable Table,
    GeneratedRowShape RowShape,
    FinalShapeResult FinalResult)
{
    public IReadOnlyList<ApplyPredicateMovementPlan> LoweredApplyPredicateMovementPlans { get; init; } = [];

    public static LoweredTable FromBuilt(TableBuildResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsBuilt || result.FinalResult is not { } finalResult)
            throw new InvalidOperationException("A lowered table product requires a complete built result.");
        return new(result.Shapes, result.Nodes, result.Table, result.RowShape, finalResult)
        {
            LoweredApplyPredicateMovementPlans = result.LoweredApplyPredicateMovementPlans
        };
    }

    public TableBuildResult ToCompatibilityResult()
    {
        return TableBuildResult.Success(Shapes, Nodes, Table, RowShape) with
        {
            LoweredApplyPredicateMovementPlans = LoweredApplyPredicateMovementPlans
        };
    }
}
