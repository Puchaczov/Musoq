namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static string FormatPlacement(PredicateEarliestPlacement placement)
    {
        return placement switch
        {
            PredicateEarliestPlacement.PreInnerJoinLeft => "pre-inner-join left",
            PredicateEarliestPlacement.PreInnerJoinRight => "pre-inner-join right",
            PredicateEarliestPlacement.PreApplyRight => "pre-apply right",
            PredicateEarliestPlacement.PostJoin => "post-join",
            PredicateEarliestPlacement.PostAggregate => "post-aggregate",
            PredicateEarliestPlacement.PostWindow => "post-window",
            _ => placement.ToString()
        };
    }
}
