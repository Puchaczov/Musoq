using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout)
    {
        return contextLayout == null
            ? []
            : contextLayout.Segments.Select(static segment => segment.Value);
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

}
