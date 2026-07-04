using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record TableBuildResult(
    bool Supported,
    IReadOnlyList<RowShape> Shapes,
    IReadOnlyList<ExecutionNode> Nodes,
    ExecutionVariable Table,
    GeneratedRowShape RowShape,
    FinalShapeResult? FinalResult,
    string UnsupportedReason)
{
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
