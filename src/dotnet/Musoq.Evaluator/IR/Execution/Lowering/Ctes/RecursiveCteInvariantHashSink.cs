using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed record RecursiveCteInvariantHashSink(
    string Name,
    ExecutionVariable Hash,
    ExecutionVariable Row,
    GeneratedRowShape CanonicalShape,
    ExecutionExpression Key,
    ExecutionTypeRef KeyType,
    ExecutionVariable SnapshotRows,
    int MaxSnapshotRows) : IDirectTableSink
{
    public ExecutionNode CreateAppend(ExecutionAppendRow append)
    {
        var values = RecursiveCteInvariantSnapshotSink.CanonicalizeValues(append, CanonicalShape);
        return new ExecutionScopedBlock(new ExecutionBlock(
        [
            new ExecutionRecursiveCteSnapshotRowGuard(Name, SnapshotRows, MaxSnapshotRows),
            new ExecutionCreateGeneratedRow(Row, CanonicalShape, values, []),
            new ExecutionHashAdd(
                Hash,
                Key,
                Row,
                KeyType,
                ExecutionClrBindingFactory.FromClr(typeof(Row)),
                CanonicalShape.TypeName,
                KeyVariableName: $"{Hash.Name}Key",
                BucketVariableName: $"{Hash.Name}Matches")
        ]));
    }

    public TableBuildResult Complete(
        IReadOnlyList<RowShape> shapes,
        IReadOnlyList<ExecutionNode> nodes,
        GeneratedRowShape workingShape,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct,
        TableProjection? finalProjection = null)
    {
        if (postOperations.Count != 0 || isDistinct || finalProjection != null)
            return TableBuildResult.Unsupported("Recursive invariant hash indexes require a direct projection.");

        return TableBuildResult.Success(
            RecursiveCteInvariantSnapshotSink.ReplaceWorkingShape(shapes, workingShape, CanonicalShape),
            nodes,
            Hash,
            CanonicalShape);
    }
}
