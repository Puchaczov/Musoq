using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    public static TakeStrategyDecision ChooseTakeStrategy(TakeNode take)
    {
        ArgumentNullException.ThrowIfNull(take);
        return take.Input switch
        {
            SkipNode { Input: SortNode sort } skip => TakeStrategyDecision.TopOffset(
                sort,
                skip,
                take,
                "Sort -> Skip -> Take can use bounded top-offset ordering."
            ),
            SortNode sort => TakeStrategyDecision.TopN(sort, take, "Sort -> Take can use bounded top-N ordering."),
            _ => TakeStrategyDecision.PlainTake(take, "Take has no adjacent sort boundary to collapse."),
        };
    }

    public static WindowStrategyDecision ChooseWindowStrategy(WindowNode window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return new WindowStrategyDecision(
            RequiresMaterialization: true,
            "Window computation requires a materialized input boundary."
        );
    }
}
