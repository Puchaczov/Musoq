using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed record RecursiveCteTableSink(
    string CteName,
    int TableIndex,
    ExecutionVariable Result,
    ExecutionVariable InputFrontier,
    ExecutionVariable Frontier,
    ExecutionVariable? Seen,
    int[] IdentityFieldIndexes,
    GeneratedRowShape CanonicalShape,
    int MaxRows) : IDirectTableSink
{
    public ExecutionNode CreateAppend(ExecutionAppendRow append)
    {
        ArgumentNullException.ThrowIfNull(append);

        if (append.Values.Count != CanonicalShape.Fields.Count)
        {
            throw new InvalidOperationException(
                $"Recursive CTE '{CteName}' append shape does not match its anchor row shape.");
        }

        var values = append.Values
            .Select((value, index) =>
            {
                var field = CanonicalShape.Fields[index];
                var expression = value.Value.ReturnType == field.Type
                    ? value.Value
                    : new ExecutionStrictCast(
                        value.Value,
                        field.Type.DisplayName,
                        field.Type);
                return value with
                {
                    FieldName = field.Name,
                    Value = expression
                };
            })
            .ToArray();
        var canonicalAppend = append with
        {
            Table = Frontier,
            RowShape = CanonicalShape,
            Values = values,
            Contexts = CanonicalShape.Contexts.Count == 0 ? [] : append.Contexts,
            ContextLayout = CanonicalShape.Contexts.Count == 0 ? null : append.ContextLayout,
            AppendMode = ExecutionAppendMode.Direct
        };

        return new ExecutionRecursiveCteAppend(
            CteName,
            Result,
            Frontier,
            Seen,
            IdentityFieldIndexes,
            MaxRows,
            canonicalAppend);
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
        {
            return TableBuildResult.Unsupported(
                $"Recursive CTE '{CteName}' branch contains unsupported table post-processing.");
        }

        var retainedShapes = shapes
            .Where(shape => shape is not GeneratedRowShape generated ||
                            !string.Equals(generated.TypeName, workingShape.TypeName, StringComparison.Ordinal))
            .Append(CanonicalShape)
            .DistinctBy(static shape => shape.Name, StringComparer.Ordinal)
            .ToArray();

        return TableBuildResult.Success(retainedShapes, nodes, Frontier, CanonicalShape);
    }
}
