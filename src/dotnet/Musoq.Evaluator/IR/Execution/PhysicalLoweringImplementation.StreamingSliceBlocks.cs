using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionBlock CreateStreamingSliceAppendBlock(
        ExecutionNode[] appendNodes,
        StreamingSlice streamingSlice)
    {
        var nodes = new List<ExecutionNode>(appendNodes.Length + 4);

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
        {
            nodes.Add(CreateCounterDecrement(streamingSlice.TakeRemaining));
            nodes.Add(new ExecutionIf(
                CreateCounterComparison(streamingSlice.TakeRemaining, BinaryOpKind.LessOrEqual, 0),
                new ExecutionBlock([new ExecutionBreak()])));
        }

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
