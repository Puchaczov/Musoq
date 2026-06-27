using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class AggregateGroupPlanner
{
    public static string CreateOwnerFieldName(int prefixLength)
    {
        return $"__owner{prefixLength.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }

    public static AggregateGroupPlan Create(
        string leafTypeName,
        IReadOnlyList<AggregateGroupKeyField> keys,
        IReadOnlyList<AggregateCapturedField> capturedFields,
        IReadOnlyList<AggregateAccumulatorField> accumulators)
    {
        var keyCount = keys.Count;
        var ownerPrefixLengths = CreateOwnerPrefixLengths(keyCount, accumulators);
        var nonLeafShapes = ownerPrefixLengths
            .Where(prefixLength => prefixLength < keyCount)
            .ToDictionary(
                static prefixLength => prefixLength,
                prefixLength => CreateGroupShape(
                    CreateLevelTypeName(leafTypeName, prefixLength, keyCount),
                    keys.Take(prefixLength).ToArray(),
                    [],
                    accumulators.Where(accumulator => accumulator.OwnerPrefixLength == prefixLength).ToArray(),
                    []));
        var ownerFields = nonLeafShapes
            .OrderBy(static pair => pair.Key)
            .Select(static pair => new AggregateGroupOwnerField(
                pair.Key,
                CreateOwnerFieldName(pair.Key),
                pair.Value))
            .ToArray();
        var leafShape = CreateGroupShape(
            leafTypeName,
            keys,
            capturedFields,
            accumulators.Where(accumulator => accumulator.OwnerPrefixLength == keyCount).ToArray(),
            ownerFields);
        var levels = ownerPrefixLengths
            .Select(prefixLength => new AggregateGroupLevelPlan(
                prefixLength,
                prefixLength == keyCount ? leafShape : nonLeafShapes[prefixLength]))
            .ToArray();

        return new AggregateGroupPlan(leafShape, levels);
    }

    private static int[] CreateOwnerPrefixLengths(
        int keyCount,
        IReadOnlyList<AggregateAccumulatorField> accumulators)
    {
        return accumulators
            .Select(static accumulator => accumulator.OwnerPrefixLength)
            .Append(keyCount)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static AggregateGroupShape CreateGroupShape(
        string typeName,
        IReadOnlyList<AggregateGroupKeyField> keys,
        IReadOnlyList<AggregateCapturedField> capturedFields,
        IReadOnlyList<AggregateAccumulatorField> accumulators,
        IReadOnlyList<AggregateGroupOwnerField> ownerFields)
    {
        return new AggregateGroupShape(typeName, keys, capturedFields, accumulators, ownerFields);
    }

    private static string CreateLevelTypeName(string leafTypeName, int prefixLength, int keyCount)
    {
        if (prefixLength == keyCount)
            return leafTypeName;

        return prefixLength == 0
            ? $"{leafTypeName}Root"
            : $"{leafTypeName}Prefix{prefixLength.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
