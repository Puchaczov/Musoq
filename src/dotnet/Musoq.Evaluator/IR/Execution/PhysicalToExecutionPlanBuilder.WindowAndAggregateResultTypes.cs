using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record AggregateSetBuildResult(
        bool Supported,
        IReadOnlyList<ExecutionNode> Nodes,
        IReadOnlyDictionary<string, AggregateAccumulatorField> TypedAccumulators,
        string UnsupportedReason)
    {
        public static AggregateSetBuildResult Success(
            IReadOnlyList<ExecutionNode> nodes,
            IReadOnlyDictionary<string, AggregateAccumulatorField> typedAccumulators)
        {
            return new AggregateSetBuildResult(true, nodes, typedAccumulators, string.Empty);
        }

        public static AggregateSetBuildResult Unsupported(string reason)
        {
            return new AggregateSetBuildResult(
                false,
                [],
                new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase),
                reason);
        }
    }

    private sealed record AggregateCapturedValue(
        string ValueName,
        Type ValueType);

    private sealed record AggregateGroupValueCaptureBuildResult(
        bool Supported,
        IReadOnlyList<ExecutionNode> Nodes,
        IReadOnlyDictionary<string, AggregateCapturedValue> CapturedValues,
        string UnsupportedReason)
    {
        public static AggregateGroupValueCaptureBuildResult Success(
            IReadOnlyList<ExecutionNode> nodes,
            IReadOnlyDictionary<string, AggregateCapturedValue> capturedValues)
        {
            return new AggregateGroupValueCaptureBuildResult(true, nodes, capturedValues, string.Empty);
        }

        public static AggregateGroupValueCaptureBuildResult Unsupported(string reason)
        {
            return new AggregateGroupValueCaptureBuildResult(
                false,
                [],
                new Dictionary<string, AggregateCapturedValue>(StringComparer.OrdinalIgnoreCase),
                reason);
        }
    }

    private sealed record WindowRegistrationBuildResult(
        bool Supported,
        WindowRegistration? Registration,
        ExecutionRankingWindowFunction? RankingFunction,
        ExecutionOffsetWindowFunction? OffsetFunction,
        MethodInfo? PluginFactory,
        string UnsupportedReason)
    {
        public static WindowRegistrationBuildResult SuccessRanking(
            WindowRegistration registration,
            ExecutionRankingWindowFunction function)
        {
            return new WindowRegistrationBuildResult(true, registration, function, null, null, string.Empty);
        }

        public static WindowRegistrationBuildResult SuccessOffset(
            WindowRegistration registration,
            ExecutionOffsetWindowFunction function)
        {
            return new WindowRegistrationBuildResult(true, registration, null, function, null, string.Empty);
        }

        public static WindowRegistrationBuildResult SuccessPlugin(
            WindowRegistration registration,
            MethodInfo factory)
        {
            return new WindowRegistrationBuildResult(true, registration, null, null, factory, string.Empty);
        }

        public static WindowRegistrationBuildResult Unsupported(string reason)
        {
            return new WindowRegistrationBuildResult(false, null, null, null, null, reason);
        }
    }

    private sealed record WindowComputationBuildResult(
        bool Supported,
        WindowRegistration? Registration,
        ExecutionNode Node,
        ExecutionVariable Results,
        string UnsupportedReason)
    {
        public static WindowComputationBuildResult Success(
            WindowRegistration registration,
            ExecutionNode node,
            ExecutionVariable results)
        {
            return new WindowComputationBuildResult(true, registration, node, results, string.Empty);
        }

        public static WindowComputationBuildResult Unsupported(string reason)
        {
            return new WindowComputationBuildResult(
                false,
                null,
                CreateEmptyMaterializationNode(),
                new ExecutionVariable(string.Empty, typeof(object)),
                reason);
        }
    }

    private sealed record OffsetWindowArgumentsBuildResult(
        bool Supported,
        ExecutionExpression Value,
        ExecutionExpression Offset,
        ExecutionExpression DefaultValue,
        string UnsupportedReason)
    {
        public static OffsetWindowArgumentsBuildResult Success(
            ExecutionExpression value,
            ExecutionExpression offset,
            ExecutionExpression defaultValue)
        {
            return new OffsetWindowArgumentsBuildResult(true, value, offset, defaultValue, string.Empty);
        }

        public static OffsetWindowArgumentsBuildResult Unsupported(string reason)
        {
            var empty = new ExecutionLiteral(null, typeof(object));
            return new OffsetWindowArgumentsBuildResult(false, empty, empty, empty, reason);
        }
    }

    private sealed record PluginWindowArgumentsBuildResult(
        bool Supported,
        ExecutionExpression Value,
        IReadOnlyList<ExecutionExpression> Arguments,
        IReadOnlyList<bool> RowScopedArguments,
        IReadOnlyList<ExecutionVariable> MethodTargets,
        string UnsupportedReason)
    {
        public static PluginWindowArgumentsBuildResult Success(
            ExecutionExpression value,
            IReadOnlyList<ExecutionExpression> arguments,
            IReadOnlyList<bool> rowScopedArguments,
            IReadOnlyList<ExecutionVariable> methodTargets)
        {
            return new PluginWindowArgumentsBuildResult(
                true,
                value,
                arguments,
                rowScopedArguments,
                methodTargets,
                string.Empty);
        }

        public static PluginWindowArgumentsBuildResult Unsupported(string reason)
        {
            return new PluginWindowArgumentsBuildResult(
                false,
                new ExecutionLiteral(null, typeof(object)),
                [],
                [],
                [],
                reason);
        }
    }
}
