using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout)
    {
        return contextLayout == null
            ? []
            : contextLayout.Segments.Select(static segment => segment.Value);
    }

    private readonly record struct NestedTransitionBinding(
        FieldBinding Binding,
        string PropertyPath);

    private sealed record TableBuildResult(
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
                    CreateColumnMetadata(table.Name, rowShape.Fields, ExecutionColumnMetadataKind.TableColumns)),
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

    private static ExecutionPlan CreateTableResultPlan(
        string identifier,
        TableBuildResult result)
    {
        return new ExecutionPlan(
            identifier,
            result.Shapes,
            new ExecutionBlock([..result.Nodes, new ExecutionReturnTable(result.Table)]),
            result.FinalResult ?? throw new InvalidOperationException("Supported table build result must expose final shape metadata."));
    }

    private static ExecutionMaterializeList CreateEmptyMaterializationNode()
    {
        return new ExecutionMaterializeList(
            new ExecutionLiteral(null, typeof(object)),
            new ExecutionVariable(string.Empty, typeof(object)));
    }

    private static ExecutionNode CreateMaterializeListNode(
        ExecutionExpression source,
        ExecutionVariable buffer,
        GeneratedRowShape? generatedRowShape = null)
    {
        return ExecutionRowStreams.CreateMaterializeList(source, buffer, generatedRowShape);
    }

    private static ExecutionNode CreateMaterializeFilteredListNode(
        ExecutionExpression source,
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        ExecutionExpression predicate,
        GeneratedRowShape? generatedRowShape = null)
    {
        return ExecutionRowStreams.CreateMaterializeFilteredList(
            source,
            buffer,
            item,
            rowAccessMode,
            predicate,
            generatedRowShape);
    }

    private static ExecutionNode CreateMaterializeExpandoListNode(
        ExecutionExpression source,
        ExecutionVariable buffer,
        ExpandoAdapterShape shape,
        ExecutionExpression? predicate)
    {
        return ExecutionRowStreams.CreateMaterializeExpandoList(source, buffer, shape, predicate);
    }

    private sealed record PostOperationResult(
        bool Supported,
        ExecutionNode Node,
        ExecutionVariable Target,
        string UnsupportedReason)
    {
        public static PostOperationResult Success(ExecutionNode node, ExecutionVariable target)
        {
            return new PostOperationResult(true, node, target, string.Empty);
        }

        public static PostOperationResult Unsupported(string reason)
        {
            var emptyTable = new ExecutionVariable(string.Empty, typeof(object));

            return new PostOperationResult(false, new ExecutionReturnTable(emptyTable), emptyTable, reason);
        }
    }
}
