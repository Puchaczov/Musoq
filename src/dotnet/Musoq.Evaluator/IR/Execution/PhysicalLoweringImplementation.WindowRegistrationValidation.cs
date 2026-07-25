using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<IReadOnlyList<WindowRegistrationBuildResult>> ResolveSupportedWindowRegistrations(
        WindowRegistration[] registrations)
    {
        if (registrations.Length == 0)
        {
            return LoweringAttempt<IReadOnlyList<WindowRegistrationBuildResult>>.Unsupported("Execution IR window lowering requires at least one window registration.");
        }

        var results = new List<WindowRegistrationBuildResult>(registrations.Length);
        var windowIndexes = new HashSet<int>();
        foreach (var registration in registrations)
        {
            if (!windowIndexes.Add(registration.WindowIndex))
            {
                return LoweringAttempt<IReadOnlyList<WindowRegistrationBuildResult>>.Unsupported(
                    $"Execution IR window lowering cannot bind duplicate window registration index {registration.WindowIndex.ToString(CultureInfo.InvariantCulture)}.");
            }

            var result = ResolveSupportedWindowRegistration(registration);
            if (!result.IsBuilt)
            {
                return LoweringAttempt<IReadOnlyList<WindowRegistrationBuildResult>>.Unsupported(
                    $"Execution IR window lowering cannot lower registration {registration.WindowIndex.ToString(CultureInfo.InvariantCulture)}. {result.UnsupportedReason}");
            }

            results.Add(result);
        }

        return LoweringAttempt<IReadOnlyList<WindowRegistrationBuildResult>>.Built(results);
    }

    private static WindowRegistrationBuildResult ResolveSupportedWindowRegistration(WindowRegistration registration)
    {
        var rankingFunction = ResolveRankingFunction(registration.FunctionName);
        if (rankingFunction != null)
            return ValidateRankingRegistration(registration, rankingFunction.Value);

        var offsetFunction = ResolveOffsetFunction(registration.FunctionName);
        if (offsetFunction != null)
            return ValidateOffsetRegistration(registration, offsetFunction.Value);

        if (IsNtileWindowFunction(registration.FunctionName))
            return ValidateNtileRegistration(registration);

        if (registration.Function != null)
            return ValidatePluginWindowRegistration(registration);

        return WindowRegistrationBuildResult.Unsupported(
            $"Execution IR window lowering cannot resolve a plugin window factory for {registration.FunctionName}.");
    }

    private static WindowRegistrationBuildResult ValidateRankingRegistration(
        WindowRegistration registration,
        ExecutionRankingWindowFunction function)
    {
        if (registration.OrderKeys.Length == 0)
        {
            return WindowRegistrationBuildResult.Unsupported(
                "Execution IR ranking window lowering requires at least one ORDER BY key.");
        }

        if (registration.ValueArguments.Length != 0)
            return WindowRegistrationBuildResult.Unsupported("Execution IR ranking window lowering does not support value arguments.");

        if (registration.ReturnType != typeof(long))
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR ranking window lowering requires a long result. Found {registration.ReturnType.Name}.");
        }

        return WindowRegistrationBuildResult.SuccessRanking(registration, function);
    }

    private static WindowRegistrationBuildResult ValidateOffsetRegistration(
        WindowRegistration registration,
        ExecutionOffsetWindowFunction function)
    {
        if (registration.OrderKeys.Length == 0)
        {
            return WindowRegistrationBuildResult.Unsupported(
                "Execution IR offset window lowering requires at least one ORDER BY key.");
        }

        if (registration.ValueArguments.Length is < 1 or > 3)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR offset window lowering expects one to three value arguments. Found {registration.ValueArguments.Length.ToString(CultureInfo.InvariantCulture)}.");
        }

        return WindowRegistrationBuildResult.SuccessOffset(registration, function);
    }

    private static WindowRegistrationBuildResult ValidateNtileRegistration(WindowRegistration registration)
    {
        if (registration.Function == null)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR Ntile window lowering requires a resolved factory method for {registration.FunctionName}.");
        }

        if (registration.OrderKeys.Length == 0)
        {
            return WindowRegistrationBuildResult.Unsupported(
                "Execution IR Ntile window lowering requires at least one ORDER BY key.");
        }

        if (registration.ValueArguments.Length != 1)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR Ntile window lowering expects one bucket argument. Found {registration.ValueArguments.Length.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (registration.ValueArguments[0].ReturnType != typeof(int))
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR Ntile window lowering requires an int bucket argument. Found {registration.ValueArguments[0].ReturnType.Name}.");
        }

        if (registration.ReturnType != typeof(long))
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR Ntile window lowering requires a long result. Found {registration.ReturnType.Name}.");
        }

        return WindowRegistrationBuildResult.SuccessPlugin(registration, registration.Function);
    }

    private static WindowRegistrationBuildResult ValidatePluginWindowRegistration(WindowRegistration registration)
    {
        if (registration.Function == null)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR plugin window lowering requires a resolved factory method for {registration.FunctionName}.");
        }

        var expectedArgumentCount = TryGetBuiltInPluginWindowArgumentCount(registration.FunctionName);
        if (expectedArgumentCount != null && registration.ValueArguments.Length != expectedArgumentCount.Value)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR plugin window lowering expects {expectedArgumentCount.Value.ToString(CultureInfo.InvariantCulture)} value argument(s) for {registration.FunctionName}. Found {registration.ValueArguments.Length.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (expectedArgumentCount == null && registration.ValueArguments.Length == 0)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR plugin window lowering expects at least one value argument for {registration.FunctionName}.");
        }

        if (registration.Frame != null)
        {
            if (!CanLowerPluginWindowFrame(registration.Frame))
            {
                return WindowRegistrationBuildResult.Unsupported(
                    "Execution IR plugin window lowering supports frames whose start bound is not after the end bound only.");
            }
        }

        return WindowRegistrationBuildResult.SuccessPlugin(registration, registration.Function);
    }

    private static bool CanLowerPluginWindowFrame(WindowFrameNode frame)
    {
        var start = GetWindowFramePosition(frame.Start);
        var end = GetWindowFramePosition(frame.End);

        return start.HasValue && end.HasValue && start.Value <= end.Value;
    }

    private static int? GetWindowFramePosition(WindowFrameBoundNode bound)
    {
        return bound.BoundType switch
        {
            WindowFrameBoundType.UnboundedPreceding => int.MinValue,
            WindowFrameBoundType.OffsetPreceding => -bound.Offset,
            WindowFrameBoundType.CurrentRow => 0,
            WindowFrameBoundType.OffsetFollowing => bound.Offset,
            WindowFrameBoundType.UnboundedFollowing => int.MaxValue,
            _ => null
        };
    }
}
