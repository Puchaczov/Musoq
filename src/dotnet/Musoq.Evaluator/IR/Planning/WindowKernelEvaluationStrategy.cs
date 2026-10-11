using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Planning;

internal static class WindowKernelEvaluationStrategy
{
    public static bool UseRunningPeers(WindowRegistration registration)
    {
        var declaration = registration.Function?.GetCustomAttribute<WindowFunctionAttribute>();
        if (registration.OrderKeys.Length == 0 || declaration?.KernelType is null ||
            declaration.ContextType is not null || declaration.RankingKind != WindowRankingKind.None ||
            (declaration.Modes & WindowEvaluationModes.Running) == 0)
            return false;
        return registration.Frame is null || registration.Frame is
        {
            FrameType: WindowFrameType.Range,
            Start.BoundType: WindowFrameBoundType.UnboundedPreceding,
            End.BoundType: WindowFrameBoundType.CurrentRow
        };
    }

    public static bool CaptureInputs(WindowRegistration registration)
    {
        if (UseRunningPeers(registration))
            return false;
        var declaration = registration.Function?.GetCustomAttribute<WindowFunctionAttribute>();
        if (declaration?.KernelType is null || declaration.ContextType is not null ||
            declaration.RankingKind != WindowRankingKind.None)
            return false;
        if (registration.Frame is not { } frame)
            return registration.OrderKeys.Length > 0;
        if (frame.Start.BoundType == WindowFrameBoundType.UnboundedPreceding &&
            (frame.End.BoundType == WindowFrameBoundType.UnboundedFollowing ||
             frame.FrameType == WindowFrameType.Rows && frame.End.BoundType == WindowFrameBoundType.CurrentRow))
            return false;
        return true;
    }
}
