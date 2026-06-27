using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<ExecutionNode> CreateWindowMaterialization(
        WindowMaterializationContext context)
    {
        if (context.SourcePipeline.Filter == null)
            return CreateUnfilteredWindowMaterialization(context);

        var predicate = ExecutionExpressionConverter.Convert(context.SourcePipeline.Filter.Predicate, context.SourceLookup);
        if (predicate is ExecutionRawExpression)
        {
            return BuildResult<ExecutionNode>.Unsupported(
                $"Execution IR ranking window lowering cannot convert pre-window filter predicate {IrExpressionPrinter.Print(context.SourcePipeline.Filter.Predicate)}.");
        }

        if (predicate.ReturnType != typeof(bool))
        {
            return BuildResult<ExecutionNode>.Unsupported(
                $"Execution IR ranking window lowering requires a boolean pre-window filter predicate. Found {predicate.ReturnType.Name}.");
        }

        if (context.SourceShape is ExpandoAdapterShape expando)
        {
            return BuildResult<ExecutionNode>.Success(
                CreateMaterializeExpandoListNode(context.SourceRows, context.Buffer, expando, predicate));
        }

        return BuildResult<ExecutionNode>.Success(
            CreateMaterializeFilteredListNode(
                context.SourceRows,
                context.Buffer,
                context.Source,
                context.RowAccessMode,
                predicate,
                context.GeneratedRowShape));
    }

    private static BuildResult<ExecutionNode> CreateUnfilteredWindowMaterialization(WindowMaterializationContext context)
    {
        if (context.SourceShape is ExpandoAdapterShape expando)
        {
            return BuildResult<ExecutionNode>.Success(
                CreateMaterializeExpandoListNode(context.SourceRows, context.Buffer, expando, null));
        }

        return BuildResult<ExecutionNode>.Success(
            CreateMaterializeListNode(context.SourceRows, context.Buffer, context.GeneratedRowShape));
    }

    private static TableBuildResult CompleteTableBuild(
        IReadOnlyList<RowShape> shapes,
        List<ExecutionNode> nodes,
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct = false,
        TableProjection? finalProjection = null)
    {
        var currentTable = resultTable;
        if (isDistinct)
        {
            if (finalProjection != null)
            {
                return TableBuildResult.Unsupported(
                    "Execution IR distinct lowering currently cannot combine hidden sort fields with final projection.");
            }

            var distinctTable = new ExecutionVariable($"{currentTable.Name}Distinct", typeof(object));
            nodes.Add(new ExecutionDistinctTable(currentTable, distinctTable));
            currentTable = distinctTable;
        }

        foreach (var operation in postOperations)
        {
            var operationResult = CreatePostOperation(operation, currentTable, resultShape);
            if (!operationResult.Supported)
                return TableBuildResult.Unsupported(operationResult.UnsupportedReason);

            nodes.Add(operationResult.Node);
            currentTable = operationResult.Target;
        }

        if (finalProjection != null)
        {
            nodes.Add(new ExecutionProjectTable(
                currentTable,
                finalProjection.Table,
                finalProjection.RowShape,
                finalProjection.FieldIndexes,
                ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(
                    finalProjection.Table,
                    currentTable),
                SerialAppendMode));
            currentTable = finalProjection.Table;
            resultShape = finalProjection.RowShape;
        }

        return TableBuildResult.Success(shapes, nodes, currentTable, resultShape);
    }
}
