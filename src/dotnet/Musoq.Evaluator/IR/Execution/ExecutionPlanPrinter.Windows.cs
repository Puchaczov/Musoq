using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatRankingNodeName(ExecutionRankingWindowFunction function)
    {
        return function switch
        {
            ExecutionRankingWindowFunction.RowNumber => "ComputeRowNumberWindow",
            ExecutionRankingWindowFunction.Rank => "ComputeRankWindow",
            ExecutionRankingWindowFunction.DenseRank => "ComputeDenseRankWindow",
            _ => $"ComputeUnknownRankingWindow({function})"
        };
    }

    private static string FormatWindowKernelPlanStrategy(ExecutionWindowKernelPlanStrategy strategy)
    {
        return strategy switch
        {
            ExecutionWindowKernelPlanStrategy.NoPartition => "no partition",
            ExecutionWindowKernelPlanStrategy.HashPartitionPerPartitionSort => "hash partition/per-partition sort",
            ExecutionWindowKernelPlanStrategy.GlobalSort => "global sort",
            ExecutionWindowKernelPlanStrategy.AlreadySortedSource => "already-sorted source",
            _ => strategy.ToString()
        };
    }

    private static string FormatRankingQualifyUpperBound(long? upperBound)
    {
        return upperBound.HasValue
            ? $" qualify <= {upperBound.Value.ToString(CultureInfo.InvariantCulture)}"
            : string.Empty;
    }

    private static string FormatWindowOrderKeys(IReadOnlyList<ExecutionWindowOrderKey> orderKeys)
    {
        return string.Join(", ", orderKeys.Select(key => $"{FormatExpression(key.Expression)}{FormatDirection(key.Descending)}{FormatNullOrdering(key.NullOrdering)}"));
    }

    private static string FormatOffsetNodeName(ExecutionOffsetWindowFunction function)
    {
        return function switch
        {
            ExecutionOffsetWindowFunction.Lag => "ComputeLagWindow",
            ExecutionOffsetWindowFunction.Lead => "ComputeLeadWindow",
            _ => $"ComputeUnknownOffsetWindow({function})"
        };
    }

    private static string FormatPluginNodeName(string functionName)
    {
        return $"Compute{NormalizeWindowFunctionName(functionName)}Window";
    }

    private static string FormatWindowAggregateKernelNodeName(
        ExecutionWindowAggregateKernelDescriptor descriptor)
    {
        return $"Compute{descriptor.Function}WindowKernel[{descriptor.Mode}]";
    }

    private static string NormalizeWindowFunctionName(string functionName)
    {
        return functionName.Replace("_", string.Empty, StringComparison.Ordinal);
    }

    private static string FormatOptionalWindowOrderKeys(IReadOnlyList<ExecutionWindowOrderKey> orderKeys)
    {
        return orderKeys.Count == 0
            ? string.Empty
            : $" order by {FormatWindowOrderKeys(orderKeys)}";
    }

    private static string FormatPluginArguments(IReadOnlyList<ExecutionExpression> arguments)
    {
        return arguments.Count == 0
            ? string.Empty
            : $" args {string.Join(", ", arguments.Select(FormatExpression))}";
    }

    private static string FormatWindowFrame(ExecutionWindowFrame? frame)
    {
        return frame == null
            ? string.Empty
            : $" frame {FormatWindowFrameKind(frame.Kind)} between {FormatWindowFrameBound(frame.Start)} and {FormatWindowFrameBound(frame.End)}";
    }

    private static string FormatWindowFrameKind(ExecutionWindowFrameKind kind)
    {
        return kind switch
        {
            ExecutionWindowFrameKind.Rows => "rows",
            ExecutionWindowFrameKind.Range => "range",
            _ => kind.ToString()
        };
    }

    private static string FormatWindowFrameBound(ExecutionWindowFrameBound bound)
    {
        return bound.Kind switch
        {
            ExecutionWindowFrameBoundKind.UnboundedPreceding => "unbounded preceding",
            ExecutionWindowFrameBoundKind.UnboundedFollowing => "unbounded following",
            ExecutionWindowFrameBoundKind.CurrentRow => "current row",
            ExecutionWindowFrameBoundKind.OffsetPreceding => $"{bound.Offset.ToString(CultureInfo.InvariantCulture)} preceding",
            ExecutionWindowFrameBoundKind.OffsetFollowing => $"{bound.Offset.ToString(CultureInfo.InvariantCulture)} following",
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null)
        };
    }

    private static string FormatPartition(ExecutionExpression? partitionKey)
    {
        return partitionKey == null ? string.Empty : $" partition by {FormatExpression(partitionKey)}";
    }

    private static string FormatOptionalPredicate(ExecutionExpression? predicate)
    {
        return predicate == null ? string.Empty : $" where {FormatExpression(predicate)}";
    }

    private static string FormatMatchTracking(ExecutionVariable? variable)
    {
        return variable == null ? string.Empty : $" [match: {variable.Name}]";
    }

}
