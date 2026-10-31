
namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanUseFusedIntOrderRankingWindow(ExecutionComputeRankingWindow ranking)
    {
        return ranking.UseIntegerOrderIndex;
    }
}
