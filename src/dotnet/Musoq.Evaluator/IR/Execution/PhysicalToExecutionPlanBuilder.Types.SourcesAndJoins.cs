using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record SingleKeyAggregateExecutionSource(
        IReadOnlyDictionary<string, RowShape> Lookup,
        IReadOnlyList<RowShape> Shapes,
        IReadOnlyList<ExecutionNode> Setup,
        Func<ExecutionBlock, ExecutionSourceLoop> CreateLoop,
        ExecutionVariable? ParallelSource = null,
        ExecutionExpression? ParallelRows = null);

    private sealed record SingleKeyAggregateExecutionSourceBuildResult(
        bool Supported,
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

    private sealed record ApplyChainSource(
        IReadOnlyList<JoinSource> Sources,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        IReadOnlyList<RowShape> Shapes);

    private sealed record ApplyChainPhysicalSource(
        PhysicalNode Source,
        bool WithOrdinality);

    private sealed record ApplyChainBuildResult(
        bool Supported,
        ApplyChainSource Chain,
        string UnsupportedReason)
    {
        public static ApplyChainBuildResult Success(ApplyChainSource chain)
        {
            return new ApplyChainBuildResult(true, chain, string.Empty);
        }

        public static ApplyChainBuildResult Unsupported(string reason)
        {
            return new ApplyChainBuildResult(
                false,
                new ApplyChainSource(
                    [],
                    new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                    []),
                reason);
        }
    }

}
