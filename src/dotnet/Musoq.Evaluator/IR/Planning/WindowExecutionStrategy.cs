using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static class WindowExecutionStrategy
{
    public static WindowRegistration[] Select(WindowRegistration[] registrations)
    {
        var captureGroups = new Dictionary<ColumnRef, int>();
        var selected = registrations
            .Select(registration =>
            {
                var declaration = registration.Function?.GetCustomAttribute<WindowFunctionAttribute>();
                var useIndex = registrations.Length == 1 && IsSupported(registration, declaration);
                var ordinalBounds =
                    declaration?.SupportsOrdinalUpperBound == true
                    && declaration.RankingKind
                        is WindowRankingKind.RowNumber
                            or WindowRankingKind.Rank
                            or WindowRankingKind.DenseRank;
                var peers = WindowKernelEvaluationStrategy.UseRunningPeers(registration, declaration);
                var capture = !peers && WindowKernelEvaluationStrategy.CaptureInputs(registration, declaration);
                var batch = capture && registration.FilterPredicate is null && declaration?.BatchKernelType is not null;
                int? captureGroup = null;
                if (
                    registrations.Length > 1
                    && (capture || declaration?.ContextType is not null)
                    && registration.FilterPredicate is null
                    && registration.ValueArguments is [ColumnRef { Stability: ColumnStability.Stable } field, ..]
                )
                {
                    if (!captureGroups.TryGetValue(field, out var group))
                    {
                        group = registration.WindowIndex;
                        captureGroups.Add(field, group);
                    }
                    captureGroup = group;
                }
                return
                    ordinalBounds == registration.UseOrdinalUpperBounds
                    && useIndex == registration.UseIntegerOrderIndex
                    && capture == registration.CaptureArguments
                    && captureGroup == registration.ValueCaptureGroup
                    && batch == registration.UseBatchEvaluation
                    && peers == registration.UseRunningPeers
                    ? registration
                    : registration with
                    {
                        UseOrdinalUpperBounds = ordinalBounds,
                        UseIntegerOrderIndex = useIndex,
                        CaptureArguments = capture,
                        ValueCaptureGroup = captureGroup,
                        UseBatchEvaluation = batch,
                        UseRunningPeers = peers,
                    };
            })
            .ToArray();
        return selected.SequenceEqual(registrations) ? registrations : selected;
    }

    private static bool IsSupported(WindowRegistration registration, WindowFunctionAttribute? declaration)
    {
        if (registration.OrderKeys is not [var key] || key.Expression.ReturnType != typeof(int))
            return false;
        if (declaration?.RankingKind is not null and not WindowRankingKind.None)
            return true;
        return declaration?.ContextType is null && registration.Frame?.FrameType == WindowFrameType.Rows;
    }
}
