using System;

namespace Musoq.Schema.Managers;

/// <summary>Provides the canonical implicit conversion costs used by callable binding.</summary>
public static class SchemaConversionClassifier
{
    /// <summary>Gets the sentinel used when no implicit conversion exists.</summary>
    public const int Incompatible = int.MaxValue;

    /// <summary>Returns the lower-is-better implicit conversion cost for two CLR types.</summary>
    public static bool TryGetCost(Type from, Type to, out int cost)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var sourceNullable = Nullable.GetUnderlyingType(from);
        var targetNullable = Nullable.GetUnderlyingType(to);

        // Nullable lifting is directional. Passing a nullable value to a
        // non-nullable parameter would require an unchecked .Value access and
        // is therefore never an implicit binding conversion.
        if (sourceNullable != null && targetNullable == null)
        {
            cost = Incompatible;
            return false;
        }

        var source = sourceNullable ?? from;
        var target = targetNullable ?? to;
        if (source == target)
        {
            cost = sourceNullable == null && targetNullable != null ? 1 : 0;
            return true;
        }

        if (target.IsGenericParameter)
        {
            cost = 999;
            return true;
        }

        if (target.IsAssignableFrom(source))
        {
            cost = GetInheritanceDistance(source, target);
            return true;
        }

        if (MethodsMetadata.ValidImplicitConversions.TryGetValue(source, out var numericTargets) &&
            numericTargets.Contains(target))
        {
            cost = MethodsMetadata.ConversionCosts.TryGetValue((source, target), out var numericCost)
                ? numericCost + (targetNullable == null ? 0 : 1)
                : Incompatible;
            return true;
        }

        cost = Incompatible;
        return false;
    }

    private static int GetInheritanceDistance(Type source, Type target)
    {
        if (source == target)
            return 0;

        var distance = 1;
        for (var current = source.BaseType; current != null; current = current.BaseType)
        {
            if (current == target)
                return distance;
            distance++;
        }

        return 20;
    }

}
