using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Planning;

internal static class WindowExecutionStrategy
{
    public static WindowRegistration[] Select(WindowRegistration[] registrations)
    {
        var selected = registrations.Select(registration =>
        {
            var useIndex = registrations.Length == 1 && IsSupported(registration);
            var declaration = registration.Function?.GetCustomAttribute<WindowFunctionAttribute>();
            var ordinalBounds = declaration?.SupportsOrdinalUpperBound == true && declaration.RankingKind is
                WindowRankingKind.RowNumber or WindowRankingKind.Rank or WindowRankingKind.DenseRank;
            var capture = WindowKernelEvaluationStrategy.CaptureInputs(registration);
            var peers = WindowKernelEvaluationStrategy.UseRunningPeers(registration);
            var batch = capture && registration.FilterPredicate is null &&
                registration.Function?.GetCustomAttribute<WindowFunctionAttribute>()?.BatchKernelType is not null;
            return ordinalBounds == registration.UseOrdinalUpperBounds &&
                   useIndex == registration.UseIntegerOrderIndex && capture == registration.CaptureArguments &&
                   batch == registration.UseBatchEvaluation && peers == registration.UseRunningPeers
                ? registration
                : registration with
                {
                    UseOrdinalUpperBounds = ordinalBounds,
                    UseIntegerOrderIndex = useIndex, CaptureArguments = capture,
                    UseBatchEvaluation = batch, UseRunningPeers = peers
                };
        }).ToArray();
        return selected.SequenceEqual(registrations) ? registrations : selected;
    }

    private static bool IsSupported(WindowRegistration registration)
    {
        if (registration.OrderKeys is not [var key] || key.Expression.ReturnType != typeof(int))
            return false;
        var declaration = registration.Function?.GetCustomAttribute<WindowFunctionAttribute>();
        if (declaration?.RankingKind is not null and not WindowRankingKind.None)
            return true;
        return declaration?.ContextType is null && registration.Frame?.FrameType == WindowFrameType.Rows;
    }
}
