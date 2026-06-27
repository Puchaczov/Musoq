using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private ExecutionBlock CreateLoopBody(
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        RowShape sourceShape,
        StreamingSlice? streamingSlice = null)
    {
        if (filter == null)
            return CreateAppendBlock(appendRow, streamingSlice);

        var condition = ExecutionExpressionConverter.Convert(filter.Predicate, sourceShape);

        return CreateFilteredAppendBlock(condition, appendRow, streamingSlice);
    }

    private ExecutionBlock CreateLoopBody(
        PhysicalFilterNode? filter,
        ExecutionAppendRecord appendRecord,
        RowShape sourceShape)
    {
        if (filter == null)
            return CreateAppendBlock(appendRecord);

        var condition = ExecutionExpressionConverter.Convert(filter.Predicate, sourceShape);
        return CreateFilteredAppendBlock(condition, appendRecord);
    }

    private ExecutionBlock CreateLoopBody(
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        StreamingSlice? streamingSlice = null)
    {
        if (filter == null)
            return CreateAppendBlock(appendRow, streamingSlice);

        var condition = ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup);

        return CreateFilteredAppendBlock(condition, appendRow, streamingSlice);
    }

    private ExecutionBlock CreateFilteredAppendBlock(
        ExecutionExpression condition,
        ExecutionAppendRow appendRow,
        StreamingSlice? streamingSlice = null)
    {
        return new ExecutionBlock([new ExecutionIf(condition, CreateAppendBlock(appendRow, streamingSlice))]);
    }

    private ExecutionBlock CreateFilteredAppendBlock(
        ExecutionExpression condition,
        ExecutionAppendRecord appendRecord)
    {
        return new ExecutionBlock([new ExecutionIf(condition, CreateAppendBlock(appendRecord))]);
    }

    private ExecutionBlock CreateAppendBlock(
        ExecutionAppendRow appendRow,
        StreamingSlice? streamingSlice = null)
    {
        var nodes = new ExecutionNode[] { appendRow };

        return streamingSlice == null
            ? new ExecutionBlock(nodes)
            : CreateStreamingSliceAppendBlock(nodes, streamingSlice);
    }

    private ExecutionBlock CreateAppendBlock(ExecutionAppendRecord appendRecord)
    {
        return new ExecutionBlock([appendRecord]);
    }

    private static ExecutionBlock CreateStreamingSliceAppendBlock(
        ExecutionNode[] appendNodes,
        StreamingSlice streamingSlice)
    {
        var nodes = new List<ExecutionNode>(appendNodes.Length + 3);

        if (streamingSlice.SkipRemaining != null)
        {
            nodes.Add(new ExecutionIf(
                CreateCounterComparison(streamingSlice.SkipRemaining, BinaryOpKind.GreaterThan, 0),
                new ExecutionBlock(
                [
                    CreateCounterDecrement(streamingSlice.SkipRemaining),
                    new ExecutionContinue()
                ])));
        }

        if (streamingSlice.TakeRemaining != null)
        {
            nodes.Add(new ExecutionIf(
                CreateCounterComparison(streamingSlice.TakeRemaining, BinaryOpKind.LessOrEqual, 0),
                new ExecutionBlock([new ExecutionBreak()])));
        }

        nodes.AddRange(appendNodes);

        if (streamingSlice.TakeRemaining != null)
            nodes.Add(CreateCounterDecrement(streamingSlice.TakeRemaining));

        return new ExecutionBlock(nodes);
    }

    private static ExecutionBinary CreateCounterComparison(
        ExecutionVariable variable,
        BinaryOpKind kind,
        int value)
    {
        return new ExecutionBinary(
            kind,
            new ExecutionVariableRead(variable),
            new ExecutionLiteral(value, typeof(int)),
            typeof(bool));
    }

    private static ExecutionAssign CreateCounterDecrement(ExecutionVariable variable)
    {
        return new ExecutionAssign(
            variable,
            new ExecutionBinary(
                BinaryOpKind.Subtract,
                new ExecutionVariableRead(variable),
                new ExecutionLiteral(1, typeof(int)),
                typeof(int)));
    }

}
