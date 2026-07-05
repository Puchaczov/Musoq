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
        return TableCompletionPlanner.Default.Complete(new TableCompletionRequest(
            shapes,
            nodes,
            resultTable,
            resultShape,
            postOperations,
            isDistinct,
            finalProjection));
    }
}
