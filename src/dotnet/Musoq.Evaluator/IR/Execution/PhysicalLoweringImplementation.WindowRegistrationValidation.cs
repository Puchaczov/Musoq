using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Evaluator.IR.Bindings;

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
        if (registration.Function?.GetCustomAttribute<WindowFunctionAttribute>()?.ContextType is not null)
            return WindowRegistrationBuildResult.SuccessPlugin(registration, registration.Function);

        var rankingFunction = RankingWindowMetadata.ResolveFunction(registration);
        if (rankingFunction != null)
            return ValidateRankingRegistration(registration, rankingFunction.Value);

        if (registration.Function != null)
            return ValidatePluginWindowRegistration(registration);

        return WindowRegistrationBuildResult.Unsupported(
            $"Execution IR window lowering cannot resolve a plugin window factory for {registration.FunctionName}.");
    }

    private static WindowRegistrationBuildResult ValidateRankingRegistration(
        WindowRegistration registration,
        ExecutionRankingWindowFunction function)
    {
        return RankingWindowMetadata.ValidateRegistration(registration, function);
    }

    private static WindowRegistrationBuildResult ValidatePluginWindowRegistration(WindowRegistration registration)
    {
        if (registration.Function?.GetCustomAttribute<WindowFunctionAttribute>()?.KernelType is not null)
            return WindowRegistrationBuildResult.SuccessPlugin(registration, registration.Function);
        return WindowRegistrationBuildResult.Unsupported(
            $"Window '{registration.FunctionName}' requires a library declaration with a concrete kernel; legacy factories are unsupported.");
    }
}
