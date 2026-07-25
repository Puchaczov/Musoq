namespace Musoq.Evaluator.IR.Execution.Lowering.Tables;

internal sealed class TableCompletionPlanner(PostOperationPlanner postOperationPlanner)
{
    public static TableCompletionPlanner Default { get; } = new(PostOperationPlanner.Default);

    public TableBuildResult Complete(TableCompletionRequest request)
    {
        var currentTable = request.ResultTable;
        var resultShape = request.ResultShape;

        if (request.IsDistinct)
        {
            if (request.FinalProjection != null)
            {
                return TableBuildResult.Unsupported(
                    "Execution IR distinct lowering currently cannot combine hidden sort fields with final projection.");
            }

            var distinctTable = new ExecutionVariable($"{currentTable.Name}Distinct", typeof(object));
            request.Nodes.Add(new ExecutionDistinctTable(currentTable, distinctTable));
            currentTable = distinctTable;
        }

        foreach (var operation in request.PostOperations)
        {
            var operationResult = postOperationPlanner.CreatePostOperation(operation, currentTable, resultShape);
            if (!operationResult.IsBuilt)
                return TableBuildResult.Unsupported(operationResult.UnsupportedReason);

            request.Nodes.Add(operationResult.Node);
            currentTable = operationResult.Target;
        }

        if (request.FinalProjection != null)
        {
            request.Nodes.Add(new ExecutionProjectTable(
                currentTable,
                request.FinalProjection.Table,
                request.FinalProjection.RowShape,
                request.FinalProjection.FieldIndexes,
                ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(
                    request.FinalProjection.Table,
                    currentTable),
                ExecutionAppendMode.Direct));
            currentTable = request.FinalProjection.Table;
            resultShape = request.FinalProjection.RowShape;
        }

        return TableBuildResult.Success(request.Shapes, request.Nodes, currentTable, resultShape);
    }
}
