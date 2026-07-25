using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private ExecutionBlock CreateLoopBody(
        PhysicalFilterNode? filter,
        ExecutionNode appendNode,
        RowShape sourceShape)
    {
        if (filter == null)
            return new ExecutionBlock([appendNode]);

        var condition = ExecutionExpressionConverter.Convert(filter.Predicate, sourceShape);
        return new ExecutionBlock([new ExecutionIf(condition, new ExecutionBlock([appendNode]))]);
    }

    private ExecutionBlock CreateLoopBody(
        PhysicalFilterNode? filter,
        ExecutionNode appendNode,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        if (filter == null)
            return new ExecutionBlock([appendNode]);

        var condition = ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup);
        return new ExecutionBlock([new ExecutionIf(condition, new ExecutionBlock([appendNode]))]);
    }

    private static ExecutionBlock CreateFilteredAppendBlock(
        ExecutionExpression condition,
        ExecutionNode appendNode) =>
        new([new ExecutionIf(condition, new ExecutionBlock([appendNode]))]);

    private static List<ExecutionNode> CreateJoinPrelude(
        JoinSources sources,
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        LoweringScope scope,
        ExecutionCapacityHint? capacityHint = null)
    {
        var nodes = new List<ExecutionNode>(sources.Left.Setup.Count + sources.Right.Setup.Count + 1);

        nodes.AddRange(sources.Left.Setup);
        nodes.AddRange(sources.Right.Setup);
        AddOutputTableCreation(nodes, resultTable, resultShape, scope, capacityHint);

        return nodes;
    }

    private ExecutionBlock CreateJoinLoopBody(
        IrExpression? joinCondition,
        PhysicalFilterNode? filter,
        ExecutionNode appendNode,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        if (joinCondition == null && filter == null)
            return new ExecutionBlock([appendNode]);

        var condition = joinCondition == null
            ? ExecutionExpressionConverter.Convert(filter!.Predicate, sourceLookup)
            : ExecutionExpressionConverter.Convert(joinCondition, sourceLookup);

        if (joinCondition != null && filter != null)
        {
            condition = new ExecutionBinary(
                BinaryOpKind.And,
                condition,
                ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup),
                typeof(bool));
        }

        return new ExecutionBlock([new ExecutionIf(condition, new ExecutionBlock([appendNode]))]);
    }

    private static ExecutionNode CreateOutputAppend(
        ExecutionAppendRow appendRow,
        LoweringScope scope) =>
        scope.DirectTableSink?.CreateAppend(appendRow) ?? appendRow;

    private static void AddOutputTableCreation(
        ICollection<ExecutionNode> nodes,
        ExecutionVariable table,
        GeneratedRowShape shape,
        LoweringScope scope,
        ExecutionCapacityHint? capacityHint = null)
    {
        if (scope.DirectTableSink == null)
            nodes.Add(CreateTable(table, shape, capacityHint));
    }

    private static TableBuildResult CompleteOutputTableBuild(
        LoweringScope scope,
        IReadOnlyList<RowShape> shapes,
        List<ExecutionNode> nodes,
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct = false,
        TableProjection? finalProjection = null)
    {
        return scope.DirectTableSink?.Complete(
                   shapes,
                   nodes,
                   resultShape,
                   postOperations,
                   isDistinct,
                   finalProjection)
               ?? CompleteTableBuild(
                   shapes,
                   nodes,
                   resultTable,
                   resultShape,
                   postOperations,
                   isDistinct,
                   finalProjection);
    }
}
