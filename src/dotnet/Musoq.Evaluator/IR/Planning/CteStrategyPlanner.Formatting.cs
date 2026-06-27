namespace Musoq.Evaluator.IR.Planning;

internal static partial class CteStrategyPlanner
{
    private static string CreateSingleUseCteMaterializationReason(CteOutputCharacteristics characteristics)
    {
        if (CanPlanReadOnceFusion(characteristics))
            return "Single-use CTE is not the terminal read-once projection candidate, so the materialized fallback is preserved.";

        return $"Single-use CTE keeps the materialized fallback because its output has {FormatCteOutputCharacteristics(characteristics)} characteristics.";
    }

    private static string FormatCteOutputCharacteristics(CteOutputCharacteristics characteristics)
    {
        if (characteristics == CteOutputCharacteristics.None)
            return CteOutputCharacteristics.None.ToString();

        return characteristics.ToString();
    }
}
