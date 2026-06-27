using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static AggregateGroupLookup[] CreateValueTupleGroupDictionaries(
        string resultTableName,
        AggregateGroupPlan aggregateGroupPlan)
    {
        var keyCount = aggregateGroupPlan.LeafShape.Keys.Count;

        return aggregateGroupPlan.Levels
            .Select(static level => level.PrefixLength)
            .Where(prefixLength => prefixLength > 0)
            .Distinct()
            .Order()
            .Select(prefixLength => new AggregateGroupLookup(
                CreateAggregateVariable(
                    resultTableName,
                    CreateValueTupleGroupDictionaryName(prefixLength - 1, keyCount),
                    typeof(object)),
                prefixLength))
            .ToArray();
    }

    private static string CreateValueTupleGroupDictionaryName(int index, int keyCount)
    {
        if (index == keyCount - 1)
            return "groups";

        return $"groupsLevel_{index.ToString(CultureInfo.InvariantCulture)}";
    }
}
