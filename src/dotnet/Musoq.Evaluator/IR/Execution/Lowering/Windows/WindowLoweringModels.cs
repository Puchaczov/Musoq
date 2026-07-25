using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Windows;

internal sealed record WindowPipeline(
    PhysicalProjectNode Project,
    WindowRegistration[] Registrations,
    IrExpression? QualifyPredicate,
    LoweringSourcePipeline Source,
    IReadOnlyList<PostOperation> PostOperations);

internal sealed record WindowQualifyTopRankPlan(
    IrExpression? Predicate,
    IReadOnlyDictionary<int, long> UpperBounds);

internal sealed record WindowMaterializationContext(
    LoweringSourcePipeline LoweringSourcePipeline,
    ExecutionExpression SourceRows,
    ExecutionVariable Buffer,
    ExecutionVariable Source,
    RowShape SourceShape,
    ExecutionRowAccessMode RowAccessMode,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    GeneratedRowShape? GeneratedRowShape);

internal sealed record WindowComputationContext(
    WindowRegistrationBuildResult RegistrationResult,
    ExecutionVariable Buffer,
    ExecutionVariable Item,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionExpression? PartitionKey,
    IReadOnlyList<ExecutionWindowOrderKey> OrderKeys,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    IReadOnlyDictionary<string, string> AggregateSourceFields,
    string ResultTableName,
    WindowResultNameMode ResultNameMode,
    WindowKeyArrayRegistry KeyArrays,
    WindowPartitionSetRegistry Partitions,
    WindowPartitionSetRegistry SortedPartitions,
    string? PartitionSignature,
    string? OrderSignature,
    string PartitionListSignature,
    string? SortedPartitionListSignature,
    IReadOnlySet<string> InPlaceSortableSortedPartitionSignatures,
    IReadOnlySet<string> SingleUsePartitionKeySignatures,
    long? QualifyUpperBound);

internal enum WindowResultNameMode
{
    Standard,
    IndexedByWindow
}

internal sealed record WindowRegistrationBuildResult(
    bool IsBuilt,
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

internal sealed record WindowComputationBuildResult(
    bool IsBuilt,
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
            new ExecutionMaterializeList(
                new ExecutionLiteral(null, typeof(object)),
                new ExecutionVariable(string.Empty, typeof(object))),
            new ExecutionVariable(string.Empty, typeof(object)),
            reason);
    }
}

internal sealed record OffsetWindowArgumentsBuildResult(
    bool IsBuilt,
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

internal sealed record PluginWindowArgumentsBuildResult(
    bool IsBuilt,
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
