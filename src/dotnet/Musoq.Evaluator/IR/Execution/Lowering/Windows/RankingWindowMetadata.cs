using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

internal static class RankingWindowMetadata
{
    public static bool RequiresPeerMetadata(WindowRegistration registration)
        => registration.Function?.GetCustomAttribute<WindowFunctionAttribute>()?.RequiresPeerMetadata ?? true;

    public static ExecutionRankingWindowFunction? ResolveFunction(WindowRegistration registration)
    {
        var kind = registration.Function?.GetCustomAttribute<WindowFunctionAttribute>()?.RankingKind;
        return kind switch
        {
            WindowRankingKind.RowNumber => ExecutionRankingWindowFunction.RowNumber,
            WindowRankingKind.Rank => ExecutionRankingWindowFunction.Rank,
            WindowRankingKind.DenseRank => ExecutionRankingWindowFunction.DenseRank,
            WindowRankingKind.PercentRank => ExecutionRankingWindowFunction.PercentRank,
            WindowRankingKind.CumeDist => ExecutionRankingWindowFunction.CumeDist,
            _ => null
        };
    }

    public static string CreateResultVariableName(
        string resultTableName,
        ExecutionRankingWindowFunction function)
    {
        return function switch
        {
            ExecutionRankingWindowFunction.RowNumber => $"{resultTableName}RowNumbers",
            ExecutionRankingWindowFunction.Rank => $"{resultTableName}Ranks",
            ExecutionRankingWindowFunction.DenseRank => $"{resultTableName}DenseRanks",
            ExecutionRankingWindowFunction.PercentRank => $"{resultTableName}PercentRanks",
            ExecutionRankingWindowFunction.CumeDist => $"{resultTableName}CumeDists",
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };
    }

    public static WindowRegistrationBuildResult ValidateRegistration(
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

        var expectedType = function is ExecutionRankingWindowFunction.PercentRank or ExecutionRankingWindowFunction.CumeDist
            ? typeof(double)
            : typeof(long);
        if (registration.Function?.GetCustomAttribute<WindowFunctionAttribute>() is { KernelType: not null })
            expectedType = registration.Function.ReturnType;
        if (registration.ReturnType != expectedType)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR {function} window lowering requires a {expectedType.Name} result. Found {registration.ReturnType.Name}.");
        }

        return WindowRegistrationBuildResult.SuccessRanking(registration, function);
    }
}
