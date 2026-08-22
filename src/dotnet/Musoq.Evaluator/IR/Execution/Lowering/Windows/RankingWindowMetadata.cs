using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal static class RankingWindowMetadata
{
    public static ExecutionRankingWindowFunction? ResolveFunction(string functionName)
    {
        var normalized = functionName.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return normalized switch
        {
            "ROWNUMBER" => ExecutionRankingWindowFunction.RowNumber,
            "RANK" => ExecutionRankingWindowFunction.Rank,
            "DENSERANK" => ExecutionRankingWindowFunction.DenseRank,
            "PERCENTRANK" => ExecutionRankingWindowFunction.PercentRank,
            "CUMEDIST" => ExecutionRankingWindowFunction.CumeDist,
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
        if (registration.ReturnType != expectedType)
        {
            return WindowRegistrationBuildResult.Unsupported(
                $"Execution IR {function} window lowering requires a {expectedType.Name} result. Found {registration.ReturnType.Name}.");
        }

        return WindowRegistrationBuildResult.SuccessRanking(registration, function);
    }
}
