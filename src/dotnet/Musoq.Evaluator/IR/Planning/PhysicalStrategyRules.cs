using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    public static AggregateStrategyDecision ChooseAggregateStrategy(int groupKeyCount, Type[] groupKeyTypes)
    {
        ArgumentNullException.ThrowIfNull(groupKeyTypes);
        if (groupKeyCount == 0)
            return new AggregateStrategyDecision(AggregateStrategyKind.AggregateOnly, "No group keys exist.");

        if (CanUseSingleKeyStrategy(groupKeyTypes))
            return new AggregateStrategyDecision(AggregateStrategyKind.SingleKey, "One resolved group key exists.");

        if (CanUseValueTupleStrategy(groupKeyTypes))
            return new AggregateStrategyDecision(AggregateStrategyKind.ValueTuple, "Multiple resolved group keys exist.");

        return new AggregateStrategyDecision(AggregateStrategyKind.Unsupported, "Aggregate group key types must be resolved before physical planning.");
    }

    public static TakeStrategyDecision ChooseTakeStrategy(TakeNode take)
    {
        ArgumentNullException.ThrowIfNull(take);
        return take.Input switch
        {
            SkipNode { Input: SortNode sort } skip => TakeStrategyDecision.TopOffset(sort, skip, take, "Sort -> Skip -> Take can use bounded top-offset ordering."),
            SortNode sort => TakeStrategyDecision.TopN(sort, take, "Sort -> Take can use bounded top-N ordering."),
            _ => TakeStrategyDecision.PlainTake(take, "Take has no adjacent sort boundary to collapse.")
        };
    }

    public static WindowStrategyDecision ChooseWindowStrategy(WindowNode window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return new WindowStrategyDecision(
            RequiresMaterialization: true,
            "Window computation requires a materialized input boundary.");
    }

    private static bool CanUseSingleKeyStrategy(Type[] keyTypes)
    {
        return keyTypes is [not null];
    }

    private static bool CanUseValueTupleStrategy(Type[] keyTypes)
    {
        if (keyTypes.Length < 2)
            return false;

        foreach (var keyType in keyTypes)
        {
            if (keyType == null)
                return false;
        }

        return true;
    }
}
