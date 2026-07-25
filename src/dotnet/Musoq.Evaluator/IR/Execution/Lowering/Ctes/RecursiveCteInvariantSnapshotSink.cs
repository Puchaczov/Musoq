using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed record RecursiveCteInvariantSnapshotSink(
    string Name,
    ExecutionVariable Snapshot,
    GeneratedRowShape CanonicalShape,
    ExecutionVariable SnapshotRows,
    int MaxSnapshotRows) : IDirectTableSink
{
    public ExecutionNode CreateAppend(ExecutionAppendRow append) => new ExecutionScopedBlock(new ExecutionBlock(
    [
        new ExecutionRecursiveCteSnapshotRowGuard(Name, SnapshotRows, MaxSnapshotRows),
        append with
        {
            Table = Snapshot,
            RowShape = CanonicalShape,
            Values = CanonicalizeValues(append, CanonicalShape),
            Contexts = [],
            ContextLayout = null,
            AppendMode = ExecutionAppendMode.Direct
        }
    ]));

    public TableBuildResult Complete(
        IReadOnlyList<RowShape> shapes,
        IReadOnlyList<ExecutionNode> nodes,
        GeneratedRowShape workingShape,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct,
        TableProjection? finalProjection = null)
    {
        if (postOperations.Count != 0 || isDistinct || finalProjection != null)
            return TableBuildResult.Unsupported("Recursive invariant snapshots require a direct projection.");

        return TableBuildResult.Success(
            ReplaceWorkingShape(shapes, workingShape, CanonicalShape),
            nodes,
            Snapshot,
            CanonicalShape);
    }

    internal static ExecutionRowValue[] CanonicalizeValues(
        ExecutionAppendRow append,
        GeneratedRowShape canonicalShape)
    {
        if (append.Values.Count != canonicalShape.Fields.Count)
            throw new InvalidOperationException("Recursive invariant projection does not match its carrier shape.");

        return append.Values.Select((value, index) =>
        {
            var field = canonicalShape.Fields[index];
            return value with
            {
                FieldName = field.Name,
                Value = value.Value.ReturnType == field.Type
                    ? value.Value
                    : new ExecutionStrictCast(value.Value, field.Type.DisplayName, field.Type)
            };
        }).ToArray();
    }

    internal static RowShape[] ReplaceWorkingShape(
        IEnumerable<RowShape> shapes,
        GeneratedRowShape workingShape,
        GeneratedRowShape canonicalShape) =>
        shapes.Where(shape => shape is not GeneratedRowShape generated ||
                              generated.TypeName != workingShape.TypeName)
            .Append(canonicalShape)
            .DistinctBy(static shape => shape.Name, StringComparer.Ordinal)
            .ToArray();
}
