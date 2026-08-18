using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
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

}
