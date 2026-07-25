using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.Sources;

internal sealed record SingleKeyAggregateExecutionSource(
    IReadOnlyDictionary<string, RowShape> Lookup,
    IReadOnlyList<RowShape> Shapes,
    IReadOnlyList<ExecutionNode> Setup,
    Func<ExecutionBlock, ExecutionSourceLoop> CreateLoop,
    ExecutionVariable? ParallelSource = null,
    ExecutionExpression? ParallelRows = null);

internal sealed record SingleKeyAggregateExecutionSourceBuildResult(
    bool IsBuilt,
    SingleKeyAggregateExecutionSource Source,
    string UnsupportedReason)
{
    public static SingleKeyAggregateExecutionSourceBuildResult Success(SingleKeyAggregateExecutionSource source)
    {
        return new SingleKeyAggregateExecutionSourceBuildResult(true, source, string.Empty);
    }

    public static SingleKeyAggregateExecutionSourceBuildResult Unsupported(string reason)
    {
        return new SingleKeyAggregateExecutionSourceBuildResult(
            false,
            new SingleKeyAggregateExecutionSource(
                new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                [],
                [],
                _ => new ExecutionForEach(
                    new ExecutionVariable(string.Empty, typeof(object)),
                    new ExecutionVariableRead(new ExecutionVariable(string.Empty, typeof(object))),
                    ExecutionBlock.Empty)),
            reason);
    }
}
