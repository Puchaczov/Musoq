using System.Linq;

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
            var distinctTable = new ExecutionVariable($"{currentTable.Name}Distinct", typeof(object));
            request.Nodes.Add(new ExecutionDistinctTable(currentTable, distinctTable)
            {
                KeyFields = request.FinalProjection is null ? [] :
                    request.FinalProjection.FieldIndexes.Select(index => resultShape.Fields[index]).ToArray()
            });
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
