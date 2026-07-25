using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<ExecutionNode> CreateWindowMaterialization(
        WindowMaterializationContext context)
    {
        if (context.LoweringSourcePipeline.Filter == null)
            return CreateUnfilteredWindowMaterialization(context);

        var predicate = ExecutionExpressionConverter.Convert(context.LoweringSourcePipeline.Filter.Predicate, context.SourceLookup);
        if (predicate.ReturnType.ResolveClrType() != typeof(bool))
        {
            return LoweringAttempt<ExecutionNode>.Unsupported(
                $"Execution IR ranking window lowering requires a boolean pre-window filter predicate. Found {predicate.ReturnType.ResolveClrType().Name}.");
        }

        if (context.SourceShape is ExpandoAdapterShape expando)
        {
            return LoweringAttempt<ExecutionNode>.Built(
                CreateMaterializeExpandoListNode(context.SourceRows, context.Buffer, expando, predicate));
        }

        return LoweringAttempt<ExecutionNode>.Built(
            CreateMaterializeFilteredListNode(
                context.SourceRows,
                context.Buffer,
                context.Source,
                context.RowAccessMode,
                predicate,
                context.GeneratedRowShape));
    }

    private static LoweringAttempt<ExecutionNode> CreateUnfilteredWindowMaterialization(WindowMaterializationContext context)
    {
        if (context.SourceShape is ExpandoAdapterShape expando)
        {
            return LoweringAttempt<ExecutionNode>.Built(
                CreateMaterializeExpandoListNode(context.SourceRows, context.Buffer, expando, null));
        }

        return LoweringAttempt<ExecutionNode>.Built(
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
